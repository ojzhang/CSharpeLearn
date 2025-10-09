using Microsoft.AspNetCore.Mvc;
using TodoList.Core.Interfaces;
using TodoList.Core.Models;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using NodaTime;

namespace TodoList.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly ITodoItemServices _todoItemService;

        public TodoItemsController(ITodoItemServices todoItemService)
        {
            _todoItemService = todoItemService;
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            // 创建一个默认用户用于演示
            var currentUser = new ApplicationUser
            {
                Id = "default-user-id",
                UserName = "default@example.com"
            };

            var todoItems = await _todoItemService.GetIncompleteItemsAsync(currentUser);
            return Ok(todoItems);
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(Guid id)
        {
            // 创建一个默认用户用于演示
            var currentUser = new ApplicationUser
            {
                Id = "default-user-id",
                UserName = "default@example.com"
            };

            var todoItem = await _todoItemService.GetItemAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            return Ok(todoItem);
        }

        // POST: api/TodoItems
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            // 创建一个默认用户用于演示
            var currentUser = new ApplicationUser
            {
                Id = "default-user-id",
                UserName = "default@example.com"
            };

            var result = await _todoItemService.AddItemAsync(todoItem, currentUser);
            if (!result)
            {
                return BadRequest("无法添加待办事项");
            }

            return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(Guid id)
        {
            // 创建一个默认用户用于演示
            var currentUser = new ApplicationUser
            {
                Id = "default-user-id",
                UserName = "default@example.com"
            };

            var result = await _todoItemService.DeleteTodoAsync(id, currentUser);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}