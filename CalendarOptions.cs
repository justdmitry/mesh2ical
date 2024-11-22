namespace SchoolHelper
{
    public class CalendarOptions
    {
        public bool Enabled { get; set; }

        public bool SkipAdditionalSources { get; set; }

        public string[] IgnoreHomeworkStrings { get; set; } = [];

        public Dictionary<string, string> ClassEmojis { get; set; } = [];
    }
}
