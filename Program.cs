using LabSyncBackbone.Data;
using LabSyncBackbone.Helpers;
using LabSyncBackbone.Mappers;
using LabSyncBackbone.Services;
using LabSyncBackbone.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using LabSyncBackbone.AppSettings;
using StackExchange.Redis;


namespace LabSyncBackbone
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Built-in OpenAPI document generation
            builder.Services.AddOpenApi();

            // Swagger UI support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "LabSync Backbone API",
                    Version = "v1",
                    Description = "Middleware API between local apps and external apps"
                });
            });


            //APSETTUNGS
            builder.Services.Configure<ExternalAppsSettings>(builder.Configuration.GetSection("ExternalApps"));
            builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
            builder.Services.Configure<PostgresSettings>(builder.Configuration.GetSection("PostgresSettings"));
            builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));
            //APPSETTINGS END

            //AES CACHE
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>().Value;
                return ConnectionMultiplexer.Connect(settings.ConnectionString!);
            });
            builder.Services.AddSingleton<ICacheService, RedisCacheService>();
            //AES CACHE ENDS

            //AES SERVICES

            builder.Services.AddScoped<ICaseStore, CaseStore>();
            builder.Services.AddScoped<SyncService>();
            builder.Services.AddHttpClient<MockExternalAppClient>((serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAppsSettings>>().Value;
                var mockSettings = settings.Apps["mock"];

                client.BaseAddress = new Uri(mockSettings.BaseUrl!);
                client.Timeout = TimeSpan.FromSeconds(mockSettings.TimeoutSeconds);
            });


            builder.Services.AddSingleton<ExternalAppRegistry>(sp =>
            {
                var registry = new ExternalAppRegistry();

                var mockClient = sp.GetRequiredService<MockExternalAppClient>();
                registry.Register("mock", mockClient);

                var mockMapper = sp.GetRequiredService<MockRequestMapper>();
                registry.RegisterMapper("mock", mockMapper);

                // Future integrations follow the same pattern:
                // registry.Register("cpm", sp.GetRequiredService<CpmClient>());
                // registry.RegisterMapper("cpm", sp.GetRequiredService<CpmRequestMapper>());

                return registry;
            });
            //AES SERVICES ENDS

            //AES POSTGRES
            builder.Services.AddDbContext<LabSyncBackboneDbContext>(options =>
            {
                var settings = builder.Configuration.GetSection("PostgresSettings").Get<PostgresSettings>();
                options.UseNpgsql(settings!.ConnectionString);
            });
            builder.Services.AddScoped<ICaseRepository, CaseRepository>();
            //AES POSTGRES ENDS

            //AES HELPERS
            builder.Services.AddScoped<SyncRequestValidator>();
            //AES HELPERS ENDS

            //AES WORKERS
            builder.Services.AddHostedService<CleanupWorker>();
            builder.Services.AddHostedService<ReconciliationWorker>();
            builder.Services.AddHostedService<RetryWorker>();
            //AES WORKERS ENDS

            //AES RECONCILIATION
            builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
            //AES RECONCILIATION ENDS

            //AES RETRY
            builder.Services.AddSingleton<RetryTrigger>();
            builder.Services.AddScoped<IFailedRequestRepository, FailedRequestRepository>();
            //AES RETRY ENDS

            //AES MAPPERS
            builder.Services.AddSingleton<MockRequestMapper>();
            //AES MAPPERS ENDS


            var app = builder.Build();

            // Auto-apply any pending database migrations on startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LabSyncBackboneDbContext>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "LabSync Backbone API v1");
                    options.RoutePrefix = "swagger";
                });
            }




            //app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}