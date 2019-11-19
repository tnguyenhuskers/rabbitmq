using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


namespace NopOrderImporter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            NLog.Extensions.Logging.ConfigSettingLayoutRenderer.DefaultConfiguration = configuration;
            
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddEventLog();
                }).UseNLog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                    services.AddHostedService<OrderService>();
                    services.AddHostedService<CustomerService>();
                    services.AddTransient<CustomerService>()
                        .Configure<WorkerOptions>(configuration.GetSection("ConnectionStrings"));
                    services.AddTransient<CustomerService>()
                        .Configure<WorkerOptions>(configuration.GetSection("AppIdentitySettings"));

                    services.AddTransient<NopToBaileyTransformer>();
                    services.AddTransient<OrderService>()
                        .Configure<WorkerOptions>(configuration.GetSection("ConnectionStrings"));
                    services.AddTransient<OrderService>()
                        .Configure<WorkerOptions>(configuration.GetSection("AppIdentitySettings"));
                    
                });
    }
}