namespace SchoolHelper
{
    public class Lesson
    {
        public long Id { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public List<string>? Homework { get; set; }

        public bool Replaced { get; set; }
    }
}
