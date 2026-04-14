using Logging.ExceptionSender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolHelper.Mesh;
using SchoolHelper.Yandex;

namespace SchoolHelper
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var app = Host
                .CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .ConfigureLogging(o => o.AddSystemdConsole())
                .ConfigureServices((context, services) =>
                {
                    services.AddTelegramExceptionSender(context.Configuration.GetSection("ExceptionSender"), b => b.TryAddTelegramProxy(context.Configuration));

                    services
                    .AddHttpClient<MeshExportService>(c => c.Timeout = TimeSpan.FromSeconds(300))
                        .AddStandardResilienceHandler();

                    services.AddScoped<StateService>();

                    services.Configure<StorageOptions>(context.Configuration.GetSection(nameof(StorageOptions)));
                    services.AddScoped<StorageService>();

                    services.Configure<CalendarOptions>(context.Configuration.GetSection(nameof(CalendarOptions)));
                    services.Configure<MealOptions>(context.Configuration.GetSection(nameof(MealOptions)));

                    services.AddTask<CalendarTask>(o => o.AutoStart(CalendarTask.Interval, TimeSpan.FromSeconds(5)).WithExceptionSender());

                    services
                        .AddHttpClient<MealTask>()
                        .AddStandardResilienceHandler();
                    services.AddTask<MealTask>(o => o.AutoStart(MealTask.Interval, TimeSpan.FromSeconds(10)).WithExceptionSender());
                })
                .Build();

            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddMemory(LogLevel.Trace);

            var logger = loggerFactory.CreateLogger(nameof(Program));
            logger.LogInformation("Started. Press Ctrl+C to break.");

            // Create/update state file.
            var stateService = app.Services.GetRequiredService<StateService>();
            await stateService.Save(s => { });

            await app.RunAsync();
        }

        private static IHttpClientBuilder TryAddTelegramProxy(this IHttpClientBuilder builder, IConfiguration configuration)
        {
            var tgProxy = configuration["TELEGRAM_API_PROXY"];

            if (!string.IsNullOrWhiteSpace(tgProxy))
            {
                builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Proxy = new System.Net.WebProxy(tgProxy) });
            }

            return builder;
        }
    }
}
