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
            services.ConfigureJwtBlacklist(_logger);
            services.ConfigureRepository(_logger);
            services.ConfigureStorage(Configuration, _logger);
            services.ConfigureAutoMapper(_logger);
            services.ConfigureSwagger(_logger);
            services.AddControllers();
        }
    }
}