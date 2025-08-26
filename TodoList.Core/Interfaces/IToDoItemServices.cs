using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TodoList.Core.Models;

namespace TodoList.Core.Interface
{
    public interface ITodoItemServices
    {
        Task<IEnumerable<TodoItem>> GetIncompleteItemsAsync(ApplicationUser currentUser);
        Task<IEnumerable<TodoItem>> GetCompleteItemsAsync(ApplicationUser currentUser);
        Task<IEnumerable<TodoItem>> GetItemsByTagAsync(ApplicationUser currentUser, string tag);
        Task<bool> AddItemAsync(TodoItem todo, ApplicationUser currentUser);
        Task<bool> UpdateItemAsync(TodoItem todo, ApplicationUser currentUser);
        bool Exists(Guid id);
        Task<bool> UpdateTodoAsync(TodoItem todo, ApplicationUser currentUser);
        Task<TodoItem> GetItemAsync(Guid id);

        Task<bool> DeleteTodoAsync(Guid id, ApplicationUser currentUser);

        Task<IEnumerable<TodoItem>> GetRecentlyAddedItemsAsync(ApplicationUser currentUser);

        Task<IEnumerable<TodoItem>> GetDueTo2DaysItems(ApplicationUser user);

        Task<IEnumerable<TodoItem>> GetMonthlyItems(ApplicationUser user, int Month);

        Task<bool> SaveFileAsync(Guid todoId, ApplicationUser currentUser, string path, long size);
    }
}