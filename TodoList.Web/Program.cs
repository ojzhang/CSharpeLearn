using TodoList.Web;

var builder = WebApplication.CreateBuilder(args);

// 使用Startup类配置服务
var startup = new Startup(builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// 配置HTTP请求管道
var env = app.Environment;
startup.Configure(app, env);

app.Run();
