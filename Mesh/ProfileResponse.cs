namespace SchoolHelper.Mesh
{
    public class ProfileResponse
    {
        public Child[] children { get; set; } = [];
        public string? hash { get; set; }
    }

    public class Child
    {
        public int user_id { get; set; }
        public int id { get; set; }
        public int contract_id { get; set; }
        public string? type { get; set; }
        public School? school { get; set; }
        public string? class_name { get; set; }
        public int class_level_id { get; set; }
        public int class_unit_id { get; set; }
        public Group[]? groups { get; set; }
        public Section[]? sections { get; set; }
        public string? contingent_guid { get; set; }
        public int service_type_id { get; set; }
    }

    public class School
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? short_name { get; set; }
        public string? county { get; set; }
        public string? principal { get; set; }
        public string? phone { get; set; }
        public int global_school_id { get; set; }
        public string? municipal_unit_name { get; set; }
    }

    public class Group
    {
        public int id { get; set; }
        public string? name { get; set; }
        public int? subject_id { get; set; }
        public bool is_fake { get; set; }
    }

    public class Section
    {
        public int id { get; set; }
        public string? name { get; set; }
        public int? subject_id { get; set; }
        public bool is_fake { get; set; }
    }
}
