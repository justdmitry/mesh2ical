using Logging.ExceptionSender;
using Mesh2Ical.Mesh;
using Mesh2Ical.Yandex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Mesh2Ical
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

                    services.Configure<MeshOptions>(context.Configuration.GetSection(nameof(MeshOptions)));

                    services.AddHttpClient<MeshExportService>(c => c.Timeout = TimeSpan.FromSeconds(300))
                        .AddPolicyHandler(Policy.WrapAsync(
                            HttpPolicyExtensions.HandleTransientHttpError().Or<Polly.Timeout.TimeoutRejectedException>().WaitAndRetryAsync(5, x => TimeSpan.FromSeconds(x * 15)),
                            Policy.TimeoutAsync<HttpResponseMessage>(15)));

                    services.Configure<StorageOptions>(context.Configuration.GetSection(nameof(StorageOptions)));
                    services.AddScoped<StorageService>();

                    services.AddTask<ExportTask>(o => o.AutoStart(ExportTask.Interval, TimeSpan.FromSeconds(5)).WithExceptionSender());
                })
                .Build();

            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(Program));
            logger.LogInformation("Started. Press Ctrl+C to break.");

            await app.RunAsync();
        }
    }
}
