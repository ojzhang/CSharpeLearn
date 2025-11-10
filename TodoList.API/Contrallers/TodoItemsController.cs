using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Linq;
using TodoList.API.Models;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using TodoList.Core.Interfaces;
using TodoList.Core.Models;
using TodoList.API.Services;

namespace TodoList.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser>? _userManager;
        private readonly ITodoItemServices _todoService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IMapper _mapper;
        private readonly ILogger<TodoItemsController> _logger;
        private readonly IJwtBlacklistService _jwtBlacklistService;

        public TodoItemsController(IServiceProvider serviceProvider,
            ITodoItemServices todoService,
            IFileStorageService fileStorageService,
            IMapper mapper,
            ILogger<TodoItemsController> logger,
            IJwtBlacklistService jwtBlacklistService)
        {
            // Resolve UserManager optionally so controller activation doesn't fail when Identity/EF
            // are not registered (e.g. dev mode without DB). Use GetService to allow null.
            _userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            _todoService = todoService;
            _fileStorageService = fileStorageService;
            _mapper = mapper;
            _logger = logger;
            _jwtBlacklistService = jwtBlacklistService;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            // Rely on the authentication middleware to validate tokens and populate HttpContext.User
            // Do NOT parse raw Authorization header here — parsing without signature validation allows forged tokens
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            {
                return null;
            }

            // First try to resolve the current user via UserManager (works for Identity-backed schemes)
            ApplicationUser? user = null;
            if (_userManager != null)
            {
                user = await _userManager.GetUserAsync(User);
                if (user != null) return user;
            }

            // Fallback to common claim types on the principal
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!string.IsNullOrEmpty(id) && _userManager != null)
            {
                user = await _userManager.FindByIdAsync(id);
                if (user != null) return user;
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email) && _userManager != null)
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user != null) return user;
            }

            return null;
        }
        [HttpGet("getitems")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetAllAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried getting all items.");
                return Unauthorized();
            }
            var items = new List<TodoItem>();
            items.AddRange(await _todoService.GetCompleteItemsAsync(user));
            items.AddRange(await _todoService.GetIncompleteItemsAsync(user));

            _logger.LogInformation($"Returned all items to {user.Email}");
            return Ok(items);
        }

        [HttpGet("complete")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetCompleteAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried getting complete items.");
                return Unauthorized();
            }
            var items = await _todoService.GetCompleteItemsAsync(user);

            _logger.LogInformation($"Returned completed items to {user.Email}");
            return Ok(items);
        }

        [HttpGet("incomplete")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetIncompleteAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried getting incomplete items.");
                return Unauthorized();
            }

            var items = await _todoService.GetIncompleteItemsAsync(user);
            _logger.LogInformation($"Returned incomplete items to {user.Email}");
            return Ok(items);
        }

        [HttpGet("bytag/{tag}")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetItemsByTag(string tag)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried getting items by tag.");
                return Unauthorized();
            }

            var items = await _todoService.GetItemsByTagAsync(user, tag);
            _logger.LogInformation($"Returned items by tag to {user.Email}");
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetItemById(Guid id)
        {
            // Ensure token is valid and not blacklisted and resolve current user
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried getting item with id {id}.");
                return Unauthorized();
            }

            var item = await _todoService.GetItemAsync(id);
            if (item == null)
            {
                _logger.LogError($"Item with id {id} not found.");
                return NotFound();
            }

            // Ensure the current user owns the requested item
            if (!string.Equals(item.UserId, user.Id, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"User {user.Email} attempted to access item {id} owned by {item.UserId}.");
                return Forbid();
            }

            _logger.LogInformation($"Returned item with id {id} to {user.Email}");
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateItem([FromBody] TodoItemDto item)
        {
            try
            {
                // Wrap logic in try/catch to capture and return detailed errors during development
                // This helps diagnose 500 errors coming from services or mapping.

                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    _logger.LogError($"Unknown user tried creating item.");
                    return Unauthorized();
                }

                if (item == null)
                {
                    _logger.LogError($"Unknown user tried creating item.");
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError($"Invalid item provided.");
                    return BadRequest(ModelState);
                }

                if (item.Done == null) item.Done = false;
                var dbItem = _mapper.Map<TodoItem>(item);
                // Ensure DuetoDateTime (EF-mapped property) is set so it maps to the DueTo column
                // Map DTO DateTime to entity Instant (DueTo) used by EF serialization property
                if (item.DuetoDateTime.HasValue)
                {
                    dbItem.DueTo = Instant.FromDateTimeUtc(DateTime.SpecifyKind(item.DuetoDateTime.Value, DateTimeKind.Utc));
                }
                // Convert comma separated tags string into IEnumerable<string>
                if (!string.IsNullOrWhiteSpace(item.Tags))
                {
                    dbItem.Tags = item.Tags.Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();
                }

                var success = await _todoService.AddItemAsync(dbItem, user);
                if (!success)
                {
                    _logger.LogError($"Failed to add item for user {user.Email}.");
                    return BadRequest("Could not add item.");
                }

                _logger.LogInformation($"User {user.Email} created item {dbItem.Title}.");
                return CreatedAtAction(nameof(GetItemById), new { id = dbItem.Id }, dbItem);
            }
            catch (Exception ex)
            {
                // Log detailed exception and return Problem with details in Development to aid debugging
                _logger.LogError(ex, "Exception while creating item");
                return Problem(detail: ex.ToString(), statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// 上传待办事项附件文件
        /// </summary>
        /// <param name="todoId">待办事项ID</param>
        /// <param name="file">要上传的文件</param>
        /// <returns>文件上传结果</returns>
        /// <response code="201">文件上传成功</response>
        /// <response code="400">请求参数错误</response>
        /// <response code="401">用户未授权</response>
        /// <response code="404">待办事项不存在</response>
        [HttpPost("{todoId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [RequestSizeLimit(52428800)]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> UploadFile(Guid todoId, [FromForm] Models.UploadFileRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogInformation($"Unkonwn User tried to upload a file.");
                return Unauthorized();
            }
            if (todoId == Guid.Empty)
            {
                _logger.LogInformation($"User with email {user.Email} tried to upload a file with an invalid todoId.");
                return BadRequest();
            }
            var item = await _todoService.GetItemAsync(todoId);
            if (item == null)
            {
                _logger.LogInformation($"User with email {user.Email} tried to upload a file for a non-existing todoId.");
                return NotFound();
            }

            var file = request?.File;
            if (file == null || file.Length == 0)
            {
                _logger.LogInformation($"User with email {user.Email} tried to upload a file with null or empty file.");
                return BadRequest(typeof(IFormFile));
            }

            var path = todoId + "\\" + file.FileName;
            await _fileStorageService.CleanDirectoryAsync(todoId.ToString());
            var saved = await _fileStorageService.SaveFileAsync(path, file.OpenReadStream());
            if (!saved)
            {
                return BadRequest("File could not be saved.");
            }
            var success = await _todoService.SaveFileAsync(todoId, user, path, file.Length);
            if (!success)
            {
                _logger.LogError("File could not be saved.");
                return BadRequest("File could not be saved.");
            }
            return CreatedAtAction(nameof(GetItemById), new { id = todoId }, new { path });
        }

        // Update item
        [HttpPut("{id}")]
        public async Task<ActionResult<TodoItem>> UpdateItemAsync([FromBody] TodoItemDto newItem, Guid id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried creating an item.");
                return Unauthorized();
            }

            if (newItem == null)
            {
                _logger.LogError($"Received null item.");
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError($"Received invalid item.");
                return BadRequest();
            }

            if (newItem.Done == null) newItem.Done = false;

            var dbItem = await _todoService.GetItemAsync(id);
            if (dbItem == null)
            {
                _logger.LogError($"Item with id {id} not found.");
                return NotFound();
            }

            // Map updated fields onto the existing entity (preserve Id/UserId)
            _mapper.Map(newItem, dbItem);
            dbItem.Id = id;
            if (dbItem.Done)
                await _todoService.UpdateDoneAsync(id, user);
            else
                await _todoService.UpdateTodoAsync(dbItem, user);

            _logger.LogInformation($"Updated item with id {dbItem.Id}.");
            return NoContent();
        }

        // Update status
        [HttpPatch("{id:Guid}/{status:bool}")]
        public async Task<ActionResult> UpdateStatus(Guid id, bool status)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried creating an item.");
                return Unauthorized();
            }

            var item = await _todoService.GetItemAsync(id);
            if (item == null)
            {
                _logger.LogError($"Item with id {id} not found.");
                return NotFound();
            }

            // false转true（前端执行）可以正常执行该判断语句；本来是true的话前端就会把该属性转为false不会执行该语句。
            if (status)
            {
                await _todoService.UpdateDoneAsync(id, user);
            }

            _logger.LogInformation($"Item with id {id} was set to DONE.");
            return NoContent();
        }

        // Delete item
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteItem(Guid id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogError($"Unknown user tried creating an item.");
                return Unauthorized();
            }

            await _todoService.DeleteTodoAsync(id, user);

            _logger.LogInformation($"Removed item with id {id}.");
            return NoContent();
        }
    }
}