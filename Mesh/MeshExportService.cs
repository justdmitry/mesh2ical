using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable S1075 // URIs should not be hardcoded

namespace Mesh2Ical.Mesh
{
    public class MeshExportService(ILogger<MeshExportService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        public const string TokenParamName = "MeshToken";

        public async IAsyncEnumerable<ClassInfo> Export()
        {
            var token = await GetToken();

            var family = await GetFamily(token);

            logger.LogInformation("Got family with {Count} children", family.children.Length);

            foreach (var child in family.children.Where(x => !string.IsNullOrEmpty(x.contingent_guid)))
            {
                var events = await GetEvents(child.contingent_guid!, token);
                var list = events.response?
                    .Select(x => new Lesson()
                    {
                        Id = x.id,
                        Start = x.start_at,
                        End = x.finish_at,
                        Name = x.subject_name,
                        Location = $"каб. {x.room_number}",
                        Homework = x.homework?.descriptions ?? [],
                    })
                    .ToList();
                var cls = new ClassInfo
                {
                    SchoolNameShort = child.school?.short_name ?? "???",
                    SchoolNameFull = child.school?.name ?? "???",
                    ClassUnitId = child.class_unit_id,
                    ClassLevel = child.class_level_id,
                    ClassName  = child.class_name ?? child.class_level_id.ToString(),
                    Lessons = list ?? [],
                };

                yield return cls;
            }
        }

        protected async Task<string> GetToken()
        {
            var oldToken = configuration[TokenParamName];

            if (string.IsNullOrEmpty(oldToken))
            {
                throw new InvalidOperationException("No token configured");
            }

            var req = new HttpRequestMessage(HttpMethod.Get, "https://school.mos.ru/v2/token/refresh?roleId=2&subsystem=2");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oldToken);

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var newToken = await resp.Content.ReadAsStringAsync();

            var text = await File.ReadAllTextAsync(Program.AppsettingsOverridesFile);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? [];
            data[TokenParamName] = newToken;
            await File.WriteAllTextAsync(Program.AppsettingsOverridesFile, JsonSerializer.Serialize(data));
            logger.LogInformation("Token updated");

            return newToken;
        }

        protected async Task<ProfileResponse> GetFamily(string token)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/family/web/v1/profile");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            return (await resp.Content.ReadFromJsonAsync<ProfileResponse>())!;
        }

        protected async Task<EventsResponse> GetEvents(string childId, string token)
        {
            var dateStart = DateTimeOffset.Now.AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dateEnd = DateTimeOffset.Now.AddDays(14).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/eventcalendar/v1/api/events?person_ids={childId}&begin_date={dateStart}&end_date={dateEnd}&expand=homework");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");
            req.Headers.TryAddWithoutValidation("X-Mes-Role", "parent");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var obj = (await resp.Content.ReadFromJsonAsync<EventsResponse>())!;

            if (obj.errors != null)
            {
                foreach (var err in obj.errors.SelectMany(x => x.Value.Select(v => v.error_description)))
                {
                    logger.LogWarning("Error: {Text}", err);
                }
            }

            return obj;
        }
    }
}
