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
using System.Collections.Generic;
using Microsoft.AspNetCore.SpaServices.Extensions;

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

            #region SPA支持
            // 添加SPA服务以支持React
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
            #endregion

            #region 基础设施
            services.AddRazorPages();
            services.AddLogging();
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            // 配置本地化
            var supportedCultures = new[] { "zh-CN", "en-US" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            // 暂时禁用 SPA 配置以避免冲突
            /*
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    // 使用代理方式替代直接调用React Development Server
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
                }
            });
            */
        }
    }
}