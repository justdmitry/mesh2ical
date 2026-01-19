using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurrentTasks;

namespace SchoolHelper
{
    public class CalendarTask(ILogger<CalendarTask> logger, IOptions<CalendarOptions> options, Mesh.MeshExportService meshService, Yandex.StorageService storageService) : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(42);

        private static readonly string HomeworkMarker = " " + char.ConvertFromUtf32(0x0365);
        private static readonly string HomeworkTitle = Emoji.House + "Домашнее задание: ";
        private static readonly string NoHomeworkText = "нет";
        private static readonly string ReplacedMarker = char.ConvertFromUtf32(0x21C5);
        private static readonly string ReplacedText = "Замена учителя.";

        private readonly CalendarOptions options = options.Value;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            // Reset to default. Will update later, if needed.
            currentTask.Options.Interval = Interval;

            if (!options.Enabled)
            {
                logger.LogDebug("Not enabled in options.");
                return;
            }

            await foreach (var cls in meshService.GetClasses(options.SkipAdditionalSources))
            {
                if (cls.Lessons.Count == 0)
                {
                    logger.LogWarning("Found 0 lessons for {ClassName}, will re-run shortly", cls.ClassName);
                    currentTask.Options.Interval = TimeSpan.FromMinutes(1);
                }
                else
                {
                    using var ms = GenerateIcal(cls);

                    var fileName = $"class{cls.ClassUnitId}.ics";

                    await storageService.Upload(fileName.ToLowerInvariant(), ms);

                    logger.LogInformation("Saved {Count} lessons of {Class} into {File}", cls.Lessons.Count, cls.ClassName, fileName);
                }
            }
        }

        protected MemoryStream GenerateIcal(ClassInfo cls)
        {
            var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, leaveOpen: true);

            writer.WriteLine("BEGIN:VCALENDAR");
            writer.WriteLine("METHOD:PUBLISH");
            writer.WriteLine("PRODID:" + typeof(CalendarTask).Assembly.FullName);
            writer.WriteLine("VERSION:2.0");

            WriteString(writer, "X-WR-CALNAME", $"Уроки {cls.ClassName}");
            WriteString(writer, "X-WR-CALDESC", $"МЭШ {cls.ClassName} / {cls.SchoolNameShort}");

            foreach (var item in cls.Lessons)
            {
                var emoji = options.ClassEmojis.FirstOrDefault(x => item.Name.Contains(x.Key, StringComparison.OrdinalIgnoreCase)).Value;
                var homework = item.Homework?
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !options.IgnoreHomeworkStrings.Contains(x, StringComparer.CurrentCultureIgnoreCase))
                    .ToList();
                var withHomework = homework != null && homework.Count > 0;
                var homeworkSymbol = withHomework ? HomeworkMarker : string.Empty;
                var replacedSymbol = item.Replaced ? ReplacedMarker : string.Empty;

                writer.WriteLine("BEGIN:VEVENT");

                WriteDateTime(writer, "DTSTAMP", DateTimeOffset.UtcNow);
                WriteString(writer, "UID", item.Id.ToString());

                WriteDateTime(writer, "DTSTART", item.Start);
                WriteString(writer, "DURATION", $"PT{(int)item.End.Subtract(item.Start).TotalMinutes}M");

                WriteString(writer, "SUMMARY", emoji + replacedSymbol + homeworkSymbol + item.Name);


                var replacedText = item.Replaced ? (ReplacedMarker + " " + ReplacedText + Environment.NewLine) : string.Empty;
                var homeworkText = withHomework
                    ? HomeworkTitle + Environment.NewLine + string.Join(Environment.NewLine, homework!)
                    : HomeworkTitle + NoHomeworkText;
                WriteString(writer, "DESCRIPTION", replacedText + homeworkText);

                WriteString(writer, "LOCATION", "каб. " + item.Location);

                writer.WriteLine("CLASS:PUBLIC");
                writer.WriteLine("TRANSP:TRANSPARENT");

                writer.WriteLine("END:VEVENT");
            }

            writer.WriteLine("END:VCALENDAR");

            writer.Flush();
            ms.Position = 0;

            return ms;
        }

        protected static void WriteString(StreamWriter writer, string property, string value)
        {
            const int MAX_LENGTH = 70;

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var valueSpan = value.Replace(",", @"\,").Replace("\r", string.Empty).Replace("\n", @"\n").AsSpan();

            writer.Write(property);
            writer.Write(":");
            if (valueSpan.Length <= MAX_LENGTH)
            {
                writer.WriteLine(valueSpan);
            }
            else
            {
                for (var i = 0; i < valueSpan.Length; i += MAX_LENGTH)
                {
                    if (i != 0)
                    {
                        writer.Write(" ");
                    }

                    if (i + MAX_LENGTH > valueSpan.Length)
                    {
                        writer.WriteLine(valueSpan[i..]);
                    }
                    else
                    {
                        writer.WriteLine(valueSpan.Slice(i, MAX_LENGTH));
                    }
                }
            }
        }

        protected static void WriteDate(StreamWriter writer, string property, DateTime value)
        {
            writer.Write(property);
            writer.Write(";VALUE=DATE:");
            writer.WriteLine(value.ToString("yyyyMMdd"));
        }

        protected static void WriteDateTime(StreamWriter writer, string property, DateTimeOffset value)
        {
            value = value.ToUniversalTime();
            writer.Write(property);
            writer.Write(":");
            writer.Write(value.ToString("yyyyMMdd"));
            writer.Write("T");
            writer.Write(value.ToString("HHmmss"));
            writer.WriteLine("Z");
        }
    }
}
