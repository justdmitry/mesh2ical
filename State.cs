namespace SchoolHelper
{
    public class State
    {
        public string? MeshToken { get; set; }

        public string? MeshRefreshToken { get; set; }

        public Dictionary<int, DateOnly> LastMealReports { get; set; } = [];
    }
}
