using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace MCPhase3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();


        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
 Host.CreateDefaultBuilder(args)
 .ConfigureWebHostDefaults(webBuilder =>
 {
     webBuilder.UseStartup<Startup>();
     webBuilder.ConfigureKestrel(options => options.AddServerHeader = false);
 })
 .ConfigureLogging(logging =>
 {
     logging.ClearProviders();
     logging.AddEventLog(new EventLogSettings()
     {
         SourceName = ".NET Runtime",
         LogName = "Application",
     });
 })
 ;

    }
}
