using System.Collections.Generic;
using TodoList.Core.Models;

namespace TodoList.Web.Models.HomeViewModels
{
    public class IndexViewModel
    {
        public IEnumerable<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    }
}