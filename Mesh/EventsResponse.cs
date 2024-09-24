namespace Mesh2Ical.Mesh
{
    public class EventsResponse
    {
        public const string SourcePlanEx = "PLAN";
        public const string SourceOutOfPlanEx = "EC";
        public const string SourceAdditionalEx = "AE";
        public const string SourceOlympiad = "OLYMPIAD";

        public static readonly Dictionary<string, string> Sources = new()
        {
            {  SourceOutOfPlanEx,  "Внеурочная деятельность" },
            {  SourceAdditionalEx,  "ДО" },
            {  "ORGANIZER",  "Выездные мероприятия" },
            {  SourcePlanEx,  "ОО" },
            {  SourceOlympiad,  "Олимпиады" },
        };

        public Response[]? response { get; set; }
        public Dictionary<string, ErrorDetail[]>? errors { get; set; }
        public int total_count { get; set; }
    }

    public class ErrorDetail
    {
        public string error_code { get; set; } = string.Empty;
        public string error_description { get; set; } = string.Empty;
        public object? details { get; set; }
    }

    public class Response
    {
        public int id { get; set; }
        ////public object? author_id { get; set; }
        ////public object? title { get; set; }
        ////public object? description { get; set; }
        public DateTimeOffset start_at { get; set; }
        public DateTimeOffset finish_at { get; set; }
        ////public object? is_all_day { get; set; }
        ////public object? conference_link { get; set; }
        ////public object? outdoor { get; set; }
        ////public object? place { get; set; }
        ////public object? place_latitude { get; set; }
        ////public object? place_longitude { get; set; }
        ////public object? created_at { get; set; }
        ////public object? updated_at { get; set; }
        ////public object? types { get; set; }
        ////public object? author_name { get; set; }
        ////public object? registration_start_at { get; set; }
        ////public object? registration_end_at { get; set; }
        public string source { get; set; } = string.Empty;
        ////public string source_id { get; set; } = string.Empty;
        ////public object? place_name { get; set; }
        ////public object? contact_name { get; set; }
        ////public object? contact_phone { get; set; }
        ////public object? contact_email { get; set; }
        ////public object? comment { get; set; }
        ////public object? need_document { get; set; }
        ////public object? type { get; set; }
        ////public object? format_name { get; set; }
        ////public object? url { get; set; }
        ////public object? project_name { get; set; }
        ////public object? subject_id { get; set; }
        public string subject_name { get; set; } = string.Empty;
        ////public string room_name { get; set; } = string.Empty;
        public string room_number { get; set; } = string.Empty;
        ////public object? replaced { get; set; }
        ////public object? replaced_teacher_id { get; set; }
        ////public object? esz_field_id { get; set; }
        ////public object? lesson_type { get; set; }
        ////public object? course_lesson_type { get; set; }
        ////public object? lesson_education_type { get; set; }
        ////public object? lesson_name { get; set; }
        ////public object? lesson_theme { get; set; }
        ////public object? activities { get; set; }
        ////public object? link_to_join { get; set; }
        ////public object? control { get; set; }
        ////public int[]? class_unit_ids { get; set; } = [];
        ////public object? class_unit_name { get; set; }
        ////public int? group_id { get; set; }
        ////public object? group_name { get; set; }
        ////public object? external_activities_type { get; set; }
        ////public object? address { get; set; }
        ////public object? place_comment { get; set; }
        ////public int? building_id { get; set; }
        ////public string building_name { get; set; } = string.Empty;
        ////public object? city_building_name { get; set; }
        ////public object? cancelled { get; set; }
        ////public bool? is_missed_lesson { get; set; }
        ////public object? is_metagroup { get; set; }
        ////public object? absence_reason_id { get; set; }
        ////public object? nonattendance_reason_id { get; set; }
        ////public object? visible_fake_group { get; set; }
        ////public object? health_status { get; set; }
        ////public object? student_count { get; set; }
        ////public object? attendances { get; set; }
        ////public bool journal_fill { get; set; }
        ////public object? comment_count { get; set; }
        ////public object? comments { get; set; }
        ////public object? data { get; set; }
        public Homework? homework { get; set; }
        ////public object? materials { get; set; }
        ////public object? marks { get; set; }
    }

    public class Homework
    {
        ////public int presence_status_id { get; set; }
        ////public int total_count { get; set; }
        ////public object? execute_count { get; set; }
        public List<string>? descriptions { get; set; }
        ////public object? link_types { get; set; }
        ////public object? materials { get; set; }
        ////public object? entries { get; set; }
    }
}
