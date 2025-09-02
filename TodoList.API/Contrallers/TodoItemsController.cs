using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TodoList.API.Models;
using TodoList.Core.Interfaces;
using TodoList.Core.Models;

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

        public TodoItemsController(UserManager<ApplicationUser> userManager, ITodoItemServices todoService, IFileStorageService fileStorageService, IMapper mapper, ILogger<TodoItemsController> logger)
        {
            _userManager = userManager;
            _todoService = todoService;
            _fileStorageService = fileStorageService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetAllAsync()
        {
            var user = await _userManager.GetUserAsync(User);
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
            var user = await _userManager.GetUserAsync(User);
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
            var user = await _userManager.GetUserAsync(User);
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
            var user = await _userManager.GetUserAsync(User);
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
            var item = await _todoService.GetItemAsync(id);
            if (item == null)
            {
                _logger.LogError($"Unknown user tried getting item with id {id}.");
                return Unauthorized();
            }
            _logger.LogInformation($"Returned item with id {id} to {item.UserId}");
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateItem([FromBody] TodoItemDto item)
        {
            var user = await _userManager.GetUserAsync(User);
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
            await _todoService.AddItemAsync(dbItem, user);

            _logger.LogInformation($"User {user.Email} created item {dbItem.Title}.");
            return CreatedAtAction(nameof(GetItemById), new { id = dbItem.Id }, dbItem);
        }

        [HttpPost("{todoId}")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)]
        public async Task<ActionResult> UploadFile(Guid todoId, [FromForm] IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
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
            var item = _todoService.GetItemAsync(todoId);
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
            var saved = _fileStorageService.SaveFileAsync(path, file.OpenReadStream());
            if (!saved.Result)
            {
                return BadRequest("File could not be saved.");
            }
            var success = _todoService.SaveFileAsync(todoId, user, path, file.Length).Result;
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
            var user = await _userManager.GetUserAsync(User);
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

            dbItem = _mapper.Map<TodoItem>(newItem);
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
            var user = await _userManager.GetUserAsync(User);
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
            var user = await _userManager.GetUserAsync(User);
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