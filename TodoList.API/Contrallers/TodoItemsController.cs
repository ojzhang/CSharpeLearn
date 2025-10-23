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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITodoItemServices _todoService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IMapper _mapper;
        private readonly ILogger<TodoItemsController> _logger;
        private readonly IJwtBlacklistService _jwtBlacklistService;

        public TodoItemsController(UserManager<ApplicationUser> userManager,
            ITodoItemServices todoService,
            IFileStorageService fileStorageService,
            IMapper mapper,
            ILogger<TodoItemsController> logger,
            IJwtBlacklistService jwtBlacklistService)
        {
            _userManager = userManager;
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
            var user = await _userManager.GetUserAsync(User);
            if (user != null) return user;

            // Fallback to common claim types on the principal
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!string.IsNullOrEmpty(id))
            {
                user = await _userManager.FindByIdAsync(id);
                if (user != null) return user;
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user != null) return user;
            }

            return null;
        }

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
                return BadRequest();
            }

            if (item.Done == null) item.Done = false;
            var dbItem = _mapper.Map<TodoItem>(item);
            // Ensure DuetoDateTime (EF-mapped property) is set so it maps to the DueTo column
            // Map DTO DateTime to entity Instant (DueTo) used by EF serialization property
            dbItem.DueTo = Instant.FromDateTimeUtc(DateTime.SpecifyKind(item.DuetoDateTime, DateTimeKind.Utc));
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

        [HttpPost("{todoId}")]
        [RequestSizeLimit(52428800)]
        public async Task<ActionResult> UploadFile(Guid todoId, [FromForm] IFormFile file)
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