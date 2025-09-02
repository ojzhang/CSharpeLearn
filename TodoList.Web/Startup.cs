using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Globalization;
using TodoList.Core.Interfaces;
using TodoList.Core.Services;
using TodoList.Core.Contexts;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Npgsql;

// using TodoList.API.Extensions;

namespace TodoList.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(environment.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{environment.EnvironmentName}.json")
                .AddEnvironmentVariables()
                .Build();
            Environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region 数据库上下文
            // 添加数据库上下文
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    Configuration.GetConnectionString("PostgreSql")));
            #endregion

            #region 单例服务
            // 添加NodaTime时钟服务
            services.AddSingleton<IClock>(SystemClock.Instance);
            #endregion

            #region 作用域服务
            // 添加TodoItem服务
            services.AddScoped<ITodoItemServices, TodoItemService>();
            #endregion

            #region 本地化支持
            // 添加本地化服务
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            
            // 配置视图本地化和数据注解本地化
            services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();
            #endregion

            #region 基础设施
            services.AddRazorPages();
            services.AddLogging();
            // 添加数据库开发者页面异常过滤器
            services.AddDatabaseDeveloperPageExceptionFilter();
            #endregion
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("zh-CN"),
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}