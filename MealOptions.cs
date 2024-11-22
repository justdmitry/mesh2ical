namespace SchoolHelper
{
    public class MealOptions
    {
        public bool Enabled { get; set; }

        public TimeSpan Offset { get; set; } = TimeSpan.FromHours(3);

        public string BotToken { get; set; } = string.Empty;

        public Dictionary<int, long> ContractToChatMapping { get; set; } = [];
    }
}
