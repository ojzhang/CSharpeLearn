using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NodaTime;
using System;
using System.Collections.Generic;
using TodoList.Core.Models;

namespace TodoList.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevController : ControllerBase
    {
        [HttpGet("todos")]
        [AllowAnonymous]
        public IActionResult GetTodos()
        {
            var items = new List<TodoItem>
            {
                new TodoItem { Id = Guid.NewGuid(), Title = "Dev Todo 1", Content = "Sample", Done = false, DueTo = Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(1)) },
                new TodoItem { Id = Guid.NewGuid(), Title = "Dev Todo 2", Content = "Sample 2", Done = true, DueTo = Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(2)) }
            };
            return Ok(items);
        }
    }
}
