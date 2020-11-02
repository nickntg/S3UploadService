using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace S3UploadService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.SetMinimumLevel(LogLevel.Trace);
                        builder.AddNLog(hostContext.Configuration);
                    });
                    services.AddHostedService<Worker>();
                    services.AddSingleton<IS3Helper, S3Helper>();
                    services.AddSingleton(hostContext.Configuration.GetSection("AppSettings").Get<AppSettings>());
                }).UseWindowsService();
    }
}
