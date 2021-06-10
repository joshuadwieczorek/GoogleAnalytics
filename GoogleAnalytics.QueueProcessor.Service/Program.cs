using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AAG.Global.Common;
using NLog.Web;
using System.Threading.Tasks;
using GoogleAnalytics.Library.HostedServices;
using System;
using Newtonsoft.Json;

namespace GoogleAnalytics.QueueProcessor.Service
{
    class Program
    {
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            // Set current directory path and error file name.
            var currentDirectoryPath = Directory.GetCurrentDirectory();
            var startupErrorLogFile = $"_startup-errors-{DateTime.Now:yyyy-MM-dd}.log";

            try
            {
                // Run the application.
                await CreateHostBuilder(args)
                    .RunConsoleAsync();
            }
            catch (Exception e)
            {
                var startupErrorLogFilePath = Path.Combine(currentDirectoryPath, startupErrorLogFile);
                using StreamWriter streamWriter = new StreamWriter(startupErrorLogFilePath, append: true);
                streamWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffffff}] ERROR | {e.Message}");
                streamWriter.WriteLine($"Object: {JsonConvert.SerializeObject(e)}{Environment.NewLine}");
            }
        }


        /// <summary>
        /// Create the host builder.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              })
              .ConfigureLogging(logging =>
              {
                  logging.ClearProviders();
                  logging.SetMinimumLevel(LogLevel.Information);
              })
              .ConfigureServices(services =>
              {
                  services.AddSingleton<ApplicationArguments>(new ApplicationArguments(args));
                  services.AddHostedService<QueueProcessorHostedService>();
              })
             .ConfigureAppConfiguration(builder =>
             {
                 builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
             })
              .UseNLog();
    }
}
