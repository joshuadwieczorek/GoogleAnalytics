using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using AAG.Global.Health;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Bugsnag.AspNet.Core;
using AAG.Global.Security;
using GoogleAnalytics.Library.Services;
using GoogleAnalytics.Library.Data;
using GoogleAnalytics.Library.Common.HealthChecks;
using GoogleAnalytics.Library.Common.MessageBroker;

namespace GoogleAnalytics.QueueProcessor.Service
{
    public class Startup
    {
        public IConfiguration Configuration { get; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
            => Configuration = configuration;


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add health checks.
            services.AddHealthChecks()
                .AddCheck<AppSettingsHealthCheck>("appSettings_check")
                .AddCheck<AccountsDbHealthCheck>("accountsDb_check")
                .AddCheck<GoogleAnalyticsDbHealthCheck>("googleAnalyticsDb_check");

            // Add logging.
            services.AddLogging(config =>
            {
                config.AddDebug();
                config.AddConsole();
            });

            // Add Bug Snag.
            services.AddBugsnag(cfg => {
                cfg.ApiKey = Configuration["BugsnagApiKey"];
                cfg.AppType = Configuration["AppType"];
                cfg.AppVersion = Configuration["AppReleaseVersion"];
                cfg.ReleaseStage = Configuration["AppReleaseStage"];
            });

            // Add other services here...
            SecurityConfiguration securityConfiguration = new SecurityConfiguration();
            Configuration.Bind("SecurityConfiguration", securityConfiguration);
            services.AddSingleton<SecurityConfiguration>(securityConfiguration);
            services.AddSingleton<CryptographyProvider>();
            services.AddTransient<AppSettingsService>();
            services.AddTransient<AccountsDbContext>();
            services.AddTransient<IMessageBroker, RabbitMQMessageBroker>();
            services.AddTransient<QueueProcessorService>();
        }


        /// <summary>
        /// Configuration.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(
              IApplicationBuilder app
            , IWebHostEnvironment env)
        {
            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    HealthCheckResponse response = new HealthCheckResponse
                    {
                        Status = report.Status.ToString(),
                        Checks = report.Entries.Select(x => new HealthCheck
                        {
                            Component = x.Key,
                            Status = x.Value.Status.ToString(),
                            Description = x.Value.Description
                        }),
                        Duration = report.TotalDuration
                    };

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                }
            });
        }
    }
}