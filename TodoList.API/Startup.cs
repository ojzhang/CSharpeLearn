using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TodoList.API.Extensions;

namespace TodoList.API
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        public Startup(IWebHostEnvironment hostingContext, ILogger<Startup> logger, IConfiguration configuration)
        {
            Configuration = configuration;
            _logger = logger;
        }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureEntityFramework(Configuration, _logger);
            services.ConfigureIdentity(_logger);
            services.ConfigureJwtAuthentication(Configuration, _logger);
            services.ConfigureRepository(_logger);
            services.ConfigureStorage(Configuration, _logger);
            services.ConfigureAutoMapper(_logger);
            services.ConfigureSwagger(_logger);
            services.AddControllers();
        }
    }
}