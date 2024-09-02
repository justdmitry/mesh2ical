﻿using System.Text;
using Microsoft.Extensions.Logging;
using RecurrentTasks;

namespace Mesh2Ical
{
    public class ExportTask(ILogger<ExportTask> logger, Mesh.MeshExportService meshService, Yandex.StorageService storageService) : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromHours(4);
        public static readonly TimeSpan ShortInterval = TimeSpan.FromHours(1);

        public static readonly List<(string Text, string Emoji)> SubjectEmojis =
            [
                ("РАЗГОВОРЫ О ВАЖНОМ", Emoji.Loudspeaker),
                ("МАТЕМАТИКА", Emoji.TriangularRuler),
                ("ЛИТЕРАТУР", Emoji.OpenBook),
                ("РУССКИЙ ЯЗЫК", Emoji.WritingHand),
                ("ОКРУЖАЮЩИЙ", Emoji.Sunflower),
                ("ИЗОБРАЗИТЕЛЬНОЕ", Emoji.ArtistPalette),
                ("МУЗЫКА", Emoji.MusicalNotes),
                ("АНГЛИЙСК", Emoji.Flag_UnitedKingdom),
                ("ФИЗИЧЕСКАЯ", Emoji.SoccerBall),
                ("ТЕХНОЛОГИЯ", Emoji.HammerAndWrench),
                ("КОМПЛЕКСНЫЙ АНАЛИЗ ТЕКСТА", Emoji.PageWithCurl),
                ("ЛИНГВИСТИЧЕСКИЙ", Emoji.SpeechBalloon),
                ("ОСНОВЫ ДУХОВНО", Emoji.Scroll),
                ("БИОЛОГИЯ", Emoji.Seedling),
                ("ГЕОГРАФИЯ", Emoji.GlobeShowingEuropeAfrica),
                ("ИСТОРИЯ", Emoji.Amphora),
            ];

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            await foreach(var cls in meshService.Export())
            {
                using var ms = GenerateIcal(cls);

                var fileName = $"class{cls.ClassUnitId}.ics";

                await storageService.Upload(fileName.ToLowerInvariant(), ms);

                logger.LogInformation("Saved {Count} lessons of {Class} into {File}", cls.Lessons.Count, cls.ClassName, fileName);
            }

            var hour = DateTime.Now.Hour;
            currentTask.Options.Interval = (hour >= 10 && hour < 17) ? ShortInterval : Interval;
        }

        protected MemoryStream GenerateIcal(ClassInfo cls)
        {
            var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, leaveOpen: true);

            writer.WriteLine("BEGIN:VCALENDAR");
            writer.WriteLine("METHOD:PUBLISH");
            writer.WriteLine("PRODID:" + typeof(ExportTask).Assembly.FullName);
            writer.WriteLine("VERSION:2.0");

            WriteString(writer, "X-WR-CALNAME", $"Уроки {cls.ClassName}");
            WriteString(writer, "X-WR-CALDESC", $"МЭШ {cls.ClassName} / {cls.SchoolNameShort}");

            foreach (var item in cls.Lessons)
            {
                writer.WriteLine("BEGIN:VEVENT");

                WriteDateTime(writer, "DTSTAMP", DateTimeOffset.UtcNow);
                WriteString(writer, "UID", item.Id.ToString());

                WriteDateTime(writer, "DTSTART", item.Start);
                WriteString(writer, "DURATION", $"PT{(int)item.End.Subtract(item.Start).TotalMinutes}M");

                var emoji = SubjectEmojis.Find(x => item.Name.Contains(x.Text, StringComparison.OrdinalIgnoreCase)).Emoji;
                item.Name = emoji + item.Name;

                WriteString(writer, "SUMMARY", item.Name);

                var homework = item.Homework.Length == 0
                    ? Emoji.House + "Домашнее задание: нет"
                    : Emoji.House + "Домашнее задание:" + Environment.NewLine + string.Join(Environment.NewLine, item.Homework);
                WriteString(writer, "DESCRIPTION", homework);

                WriteString(writer, "LOCATION", item.Location);

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

            value = value.Replace(",", @"\,").Replace(Environment.NewLine, @"\n");

            writer.Write(property);
            writer.Write(":");
            if (value.Length <= MAX_LENGTH)
            {
                writer.WriteLine(value);
            }
            else
            {
                for (var i = 0; i < value.Length; i += MAX_LENGTH)
                {
                    if (i != 0)
                    {
                        writer.Write(" ");
                    }

                    if (i + MAX_LENGTH > value.Length)
                    {
                        writer.WriteLine(value.Substring(i));
                    }
                    else
                    {
                        writer.WriteLine(value.Substring(i, MAX_LENGTH));
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
