namespace SchoolHelper
{
    public class ClassInfo
    {
        public int ClassUnitId { get; set; }

        public int ClassLevel { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string SchoolNameShort { get; set; } = string.Empty;

        public string SchoolNameFull { get; set; } = string.Empty;

        public List<Lesson> Lessons { get; set; } = [];
    }
}
