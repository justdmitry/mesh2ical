using Logging.ExceptionSender;
using SchoolHelper.Mesh;
using SchoolHelper.Yandex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace SchoolHelper
{
    public static class Program
    {
        public const string AppsettingsOverridesFile = "appsettings.Overrides.json";

        public static async Task Main(string[] args)
        {
            if (!File.Exists(AppsettingsOverridesFile))
            {
                await File.WriteAllTextAsync(AppsettingsOverridesFile, "{}");
            }

            var app = Host
                .CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .ConfigureAppConfiguration(c => c.AddJsonFile(AppsettingsOverridesFile, false, true))
                .ConfigureLogging(o => o.AddSystemdConsole())
                .ConfigureServices((context, services) =>
                {
                    services.AddTelegramExceptionSender(context.Configuration.GetSection("ExceptionSender"));

                    services.AddHttpClient<MeshExportService>(c => c.Timeout = TimeSpan.FromSeconds(300))
                        .AddPolicyHandler(Policy.WrapAsync(
                            HttpPolicyExtensions.HandleTransientHttpError().Or<Polly.Timeout.TimeoutRejectedException>().WaitAndRetryAsync(5, x => TimeSpan.FromSeconds(x * 15)),
                            Policy.TimeoutAsync<HttpResponseMessage>(15)));

                    services.Configure<StorageOptions>(context.Configuration.GetSection(nameof(StorageOptions)));
                    services.AddScoped<StorageService>();

                    services.Configure<CalendarOptions>(context.Configuration.GetSection(nameof(CalendarOptions)));

                    services.AddTask<CalendarTask>(o => o.AutoStart(CalendarTask.Interval, TimeSpan.FromSeconds(5)).WithExceptionSender());
                })
                .Build();

            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddMemory(LogLevel.Trace);

            var logger = loggerFactory.CreateLogger(nameof(Program));
            logger.LogInformation("Started. Press Ctrl+C to break.");

            await app.RunAsync();
        }
    }
}
