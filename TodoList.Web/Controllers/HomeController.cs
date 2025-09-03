using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoList.Web.Models;
using TodoList.Core.Interfaces;
using TodoList.Core.Models;
using NodaTime;
using System.Threading.Tasks;
using TodoList.Web.Models.HomeViewModels;

namespace TodoList.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ITodoItemServices _todoItemService;
    private readonly IClock _clock;

    public HomeController(ILogger<HomeController> logger, ITodoItemServices todoItemService, IClock clock)
    {
        _logger = logger;
        _todoItemService = todoItemService;
        _clock = clock;
    }

    public async Task<IActionResult> Index()
    {
        // 创建一个默认用户用于演示
        var currentUser = new ApplicationUser
        {
            Id = "default-user-id",
            UserName = "default@example.com"
        };

        // 获取未完成的待办事项
        var todoItems = await _todoItemService.GetIncompleteItemsAsync(currentUser);
        
        var model = new IndexViewModel
        {
            TodoItems = todoItems
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult TodoList()
    {
        var model = new TodoItemCreateViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TodoList(TodoItemCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 创建一个默认用户用于演示
        var currentUser = new ApplicationUser
        {
            Id = "default-user-id",
            UserName = "default@example.com"
        };

        // 创建TodoItem对象
        var todoItem = new TodoItem
        {
            Title = model.ItemTitle,
            Content = model.ItemContent,
            DueTo = model.DueToDateTime.HasValue 
                ? Instant.FromDateTimeUtc(model.DueToDateTime.Value.ToUniversalTime()) 
                : Instant.MinValue,
            Tags = string.IsNullOrEmpty(model.ItemTags) ? new string[0] : model.ItemTags.Split(',')
        };

        // 添加到数据库
        var result = await _todoItemService.AddItemAsync(todoItem, currentUser);

        if (result)
        {
            // 添加成功后重定向到首页
            return RedirectToAction("Index");
        }

        // 如果添加失败，返回视图并显示错误
        ModelState.AddModelError("", "添加待办事项时发生错误");
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}