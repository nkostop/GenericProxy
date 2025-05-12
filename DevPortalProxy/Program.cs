using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System;

namespace Nbg.NetCore.DevPortal.Proxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower();
            var logger = NLog.LogManager.Setup()
                .LoadConfiguration(new NLog.Config.XmlLoggingConfiguration("nlog.config", true))
                .GetCurrentClassLogger();
        


            try
            {
                logger.Info("Web Api Starting");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Host terminated unexpectedly");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                // .ConfigureAppConfiguration((_, configuration) =>

                // {

                //     configuration.AddSqlServerNoWatcher();

                // })
                  .ConfigureLogging(builder =>
                        {
                            builder.ClearProviders();
                        })
                        .UseNLog()
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseStartup<Startup>()
                            .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                            .CaptureStartupErrors(true);
                        });
                      
        }
    }
}