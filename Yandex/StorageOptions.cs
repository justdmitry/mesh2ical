namespace Mesh2Ical.Yandex
{
    public class StorageOptions
    {
        public string Endpoint { get; set; } = "https://s3.yandexcloud.net";

        public string Region { get; set; } = "ru-central1";

        public string BucketName { get; set; } = string.Empty;

        public string KeyId { get; set; } = string.Empty;

        public string KeySecret { get; set; } = string.Empty;
    }
}
