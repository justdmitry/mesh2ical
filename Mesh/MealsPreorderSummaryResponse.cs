namespace SchoolHelper.Mesh
{
    public class MealsPreorderSummaryResponse
    {
        public int forbiddenDays { get; set; }
        public long orderSum14Days { get; set; }
        public long orderSum3Days { get; set; }
        public Schedule[] schedule { get; set; }
    }

    public class Schedule
    {
        public string onDate { get; set; }
        public bool orderAllowed { get; set; }
        public int orderCount { get; set; }
        public long orderSummary { get; set; }
        public Address address { get; set; }
    }

    public class Address
    {
        public int id { get; set; }
        public string organizationName { get; set; }
        public string text { get; set; }
    }
}
