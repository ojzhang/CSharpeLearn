using Microsoft.EntityFrameworkCore;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoList.Core.Contexts;
using TodoList.Core.Interfaces;
using TodoList.Core.Models;

namespace TodoList.Core.Services
{
    /// <summary>
    /// 待办事项服务类，提供对TodoItem实体的各种操作方法
    /// 包括添加、更新、查询和删除待办事项等功能
    /// </summary>
    public class TodoItemService : ITodoItemServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IClock _clock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="clock">NodaTime时钟服务，用于获取当前时间</param>
        public TodoItemService(ApplicationDbContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public async Task<IEnumerable<TodoItem>> GetItemsByTagAsync(ApplicationUser currentUser, string tag)
        {
            return await _context.TodoItem
                .Where(t => t.Tags.Contains(tag))
                .ToArrayAsync();
        }

        /// <summary>
        /// 添加新的待办事项
        /// </summary>
        /// <param name="todo">待添加的待办事项对象</param>
        /// <param name="user">当前用户</param>
        /// <returns>添加成功返回true，否则返回false</returns>
        public async Task<bool> AddItemAsync(TodoItem todo, ApplicationUser user)
        {
            // 设置待办事项的唯一标识符
            todo.Id = Guid.NewGuid();
            // 新添加的待办事项默认为未完成状态
            todo.Done = false;
            // 设置添加时间
            todo.Added = _clock.GetCurrentInstant();
            // 关联当前用户
            todo.UserId = user.Id;
            // 确保DueTo属性也被正确设置
            // 如果传入的todo对象中DueTo属性为默认值，则不进行特殊处理
            // 只有当DueTo属性被设置为有效值时才保持该值
            if (todo.DueTo == NodaTime.Instant.MinValue)
            {
                // 如果DueTo是默认值，则保持默认值，不需要额外设置
                todo.DueTo = NodaTime.Instant.MinValue;
            }
            // 如果DueTo已经被设置为有效值，则保持该值不变

            // 初始化文件信息
            todo.File = new TodoItemFile
            {
                TodoId = todo.Id,
                Path = "",
                Size = 0
            };
            _context.TodoItem.Add(todo);

            var saved = await _context.SaveChangesAsync();
            return saved > 0;
        }

        /// <summary>
        /// 获取指定用户的所有未完成待办事项
        /// </summary>
        /// <param name="user">当前用户</param>
        /// <returns>未完成的待办事项集合</returns>
        public async Task<IEnumerable<TodoItem>> GetIncompleteItemsAsync(ApplicationUser user)
        {
            return await _context.TodoItem
                .Where(t => !t.Done && t.UserId == user.Id)
                .ToArrayAsync();
        }

        public async Task<IEnumerable<TodoItem>> GetCompleteItemsAsync(ApplicationUser user)
        {
            return await _context.TodoItem
                .Where(t => t.Done && t.UserId == user.Id)
                .ToArrayAsync();
        }

        public bool Exists(Guid id)
        {
            return _context.TodoItem
                .Any(t => t.Id == id);
        }

        public async Task<bool> UpdateDoneAsync(Guid id, ApplicationUser user)
        {
            var todo = await _context.TodoItem
                .Where(t => t.Id == id && t.UserId == user.Id)
                .SingleOrDefaultAsync();

            if (todo == null) return false;

            todo.Done = !todo.Done;

            var saved = await _context.SaveChangesAsync();
            return saved == 1;
        }

        public async Task<bool> UpdateItemAsync(TodoItem editedTodo, ApplicationUser user)
        {
            var todo = await _context.TodoItem
                .Where(t => t.Id == editedTodo.Id && t.UserId == user.Id)
                .SingleOrDefaultAsync();

            if (todo == null) return false;

            todo.Title = editedTodo.Title;
            todo.Content = editedTodo.Content;
            todo.Tags = editedTodo.Tags;

            var saved = await _context.SaveChangesAsync();
            return saved == 1;
        }

        public async Task<bool> UpdateTodoAsync(TodoItem editedTodo, ApplicationUser user)
        {
            var todo = await _context.TodoItem
                .Where(t => t.Id == editedTodo.Id && t.UserId == user.Id)
                .SingleOrDefaultAsync();

            if (todo == null) return false;

            todo.Title = editedTodo.Title;
            todo.Content = editedTodo.Content;
            todo.Tags = editedTodo.Tags;

            var saved = await _context.SaveChangesAsync();
            return saved == 1;
        }

        public async Task<TodoItem> GetItemAsync(Guid id)
        {
            return await _context.TodoItem
                .Include(t => t.File)
                .Where(t => t.Id == id)
                .SingleOrDefaultAsync();
        }

        public async Task<bool> DeleteTodoAsync(Guid id, ApplicationUser currentUser)
        {
            var todo = await _context.TodoItem
                .Include(t => t.File)
                .Where(t => t.Id == id && t.UserId == currentUser.Id)
                .SingleOrDefaultAsync();

            _context.TodoItem.Remove(todo);
            _context.TodoItemFile.Remove(todo.File);

            var deleted = await _context.SaveChangesAsync();
            return deleted > 0;
        }

        /// <summary>
        /// 获取最近一天内添加的待办事项
        /// </summary>
        /// <param name="currentUser">当前用户</param>
        /// <returns>最近一天内添加的未完成待办事项集合</returns>
        public async Task<IEnumerable<TodoItem>> GetRecentlyAddedItemsAsync(ApplicationUser currentUser)
        {
            // 计算一天前的时间点
            var yesterday = _clock.GetCurrentInstant().Minus(Duration.FromDays(1));
            // 将NodaTime的Instant转换为DateTime，用于数据库查询
            var yesterdayDateTime = yesterday.ToDateTimeUtc();
            return await _context.TodoItem
                .Where(t => t.UserId == currentUser.Id && !t.Done
                && t.AddedDateTime >= yesterdayDateTime)
                .ToArrayAsync();
        }

        /// <summary>
        /// 获取未来两天内到期的待办事项
        /// </summary>
        /// <param name="user">当前用户</param>
        /// <returns>未来两天内到期的未完成待办事项集合</returns>
        public async Task<IEnumerable<TodoItem>> GetDueTo2DaysItems(ApplicationUser user)
        {
            // 计算从现在起一天后的时间点作为阈值
            var dueToThreshold = _clock.GetCurrentInstant().Plus(Duration.FromDays(1));
            // 将NodaTime的Instant转换为DateTime，用于数据库查询
            var dueToThresholdDateTime = dueToThreshold.ToDateTimeUtc();
            return await _context.TodoItem
                .Where(t => t.UserId == user.Id && !t.Done
                && t.DuetoDateTime <= dueToThresholdDateTime)
                .ToArrayAsync();
        }

        /// <summary>
        /// 获取指定月份内到期的待办事项
        /// </summary>
        /// <param name="user">当前用户</param>
        /// <param name="month">月份（1-12）</param>
        /// <returns>指定月份内到期的未完成待办事项集合</returns>
        public async Task<IEnumerable<TodoItem>> GetMonthlyItems(ApplicationUser user, int month)
        {
            return await _context.TodoItem
                .Where(t => t.UserId == user.Id && !t.Done)
                // 筛选DuetoDateTime不为null且月份匹配的待办事项
                .Where(t => t.DuetoDateTime != null && t.DuetoDateTime.Value.Month == month)
                .ToArrayAsync();
        }

        public async Task<bool> SaveFileAsync(Guid todoId, ApplicationUser currentUser, string path, long size)
        {
            var todo = await _context.TodoItem.Include(t => t.File)
                .Where(t => t.Id == todoId && t.UserId == currentUser.Id)
                .SingleOrDefaultAsync();

            if (todo == null) return false;

            todo.File.Path = path;
            todo.File.Size = size;
            todo.File.TodoId = todo.Id;

            var changes = await _context.SaveChangesAsync();
            return changes > 0;
        }
    }
}
