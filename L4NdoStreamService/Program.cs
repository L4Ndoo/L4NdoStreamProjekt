using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace L4NdoStreamService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                //.ConfigureLogging(logging =>
                //{
                //    logging.ClearProviders();
                //    logging.AddConsole();
                //    logging.AddConsoleFormatter<ConsoleFormatter, ConsoleFormatterOptions>(options =>
                //    {
                //        options.IncludeScopes = true;
                //        options.TimestampFormat = "hh:mm:ss";
                //    });
                //})
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseUrls(new string[] {"http://*:5000", "https://*:5001"})
                        .ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.AddConsole();
                        })
                        .UseStartup<Startup>();
                });
    }
}
