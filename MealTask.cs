using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTelegramBotApi;
using RecurrentTasks;

namespace SchoolHelper
{
    public class MealTask(ILogger<CalendarTask> logger, IOptions<MealOptions> options, Mesh.MeshExportService meshService, HttpClient httpClient, IConfiguration configuration) : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(42);
        public static readonly CultureInfo culture = new("ru-RU");

        private readonly MealOptions options = options.Value;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (!options.Enabled)
            {
                logger.LogDebug("Not enabled in options.");
                return;
            }

            if (string.IsNullOrWhiteSpace(options.BotToken))
            {
                logger.LogError("BotToken not set.");
                return;
            }

            var now = DateTimeOffset.UtcNow.ToOffset(options.Offset);
            if (now.Hour < 20)
            {
                logger.LogDebug("Too early to report for tomorrow.");
                return;
            }

            var next = DateOnly.FromDateTime(now.AddDays(1).Date);
            if (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
            {
                logger.LogDebug("Tomorrow is Saturday of Sunday, do not report these days.");
                return;
            }

            if (options.ContractToChatMapping.Count == 0)
            {
                return;
            }

            var bot = new TelegramBot(options.BotToken, httpClient);
            var dateAsText = next.ToString("d", culture);

            foreach (var (contractId, chatId) in options.ContractToChatMapping)
            {
                var keyName = $"LASTMEALREPORT_{contractId}";

                var lastReport = configuration.GetValue<DateOnly>(keyName);
                if (next == lastReport)
                {
                    logger.LogDebug("Meal of {ContractId} for {Date:d} has already been reported", contractId, next);
                    continue;
                }

                var found = false;
                var meals = meshService.GetMeals(contractId, next);
                await foreach (var meal in meals)
                {
                    var msg = $"*{dateAsText.EscapeMarkdownV2()}, {meal.Name.EscapeMarkdownV2()}*:{Environment.NewLine}{meal.Content.EscapeMarkdownV2()}";
                    await bot.SendMessage(new() { ChatId = chatId, Text = msg, ParseMode = NetTelegramBotApi.Types.ParseMode.MarkdownV2 });
                    logger.LogDebug("Meal {Name} of {ContractId} for {Date:d} reported to {ChatId}", meal.Name, contractId, next, chatId);
                    found = true;
                }

                if (!found)
                {
                    var msg = $"{dateAsText.EscapeMarkdownV2()}: питание не заказано";
                    await bot.SendMessage(new() { ChatId = chatId, Text = msg });
                    logger.LogDebug("No-meal of {ContractId} for {Date:d} reported to {ChatId}", contractId, next, chatId);
                }

                var text = await File.ReadAllTextAsync(Program.AppsettingsOverridesFile);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? [];
                data[keyName] = next.ToString(CultureInfo.InvariantCulture);
                await File.WriteAllTextAsync(Program.AppsettingsOverridesFile, JsonSerializer.Serialize(data));
            }
        }
    }
}
