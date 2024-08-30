using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable S1075 // URIs should not be hardcoded

namespace Mesh2Ical.Mesh
{
    public class MeshExportService(ILogger<MeshExportService> logger, HttpClient httpClient, IOptionsSnapshot<MeshOptions> options)
    {
        private readonly MeshOptions options = options.Value;

        public async IAsyncEnumerable<(string FileName, List<SchoolEvent> Events)> Export()
        {
            var token = await RefreshToken();

            foreach (var pair in options.Child2File)
            {
                var events = await GetEvents(pair.Key, token);
                var list = events.response
                    .Select(x => new SchoolEvent()
                    {
                        Id = x.id,
                        Start = x.start_at,
                        End = x.finish_at,
                        Name = x.subject_name,
                        Location = $"каб. {x.room_number}",
                    })
                    .ToList();
                yield return (pair.Value, list);
            }
        }

        protected async Task<string> RefreshToken()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://school.mos.ru/v2/token/refresh?roleId=2&subsystem=2");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Token);

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var newToken = await resp.Content.ReadAsStringAsync();

            var text = await File.ReadAllTextAsync(Program.AppsettingsOverridesFile);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? [];
            data["MeshOptions:Token"] = newToken;
            await File.WriteAllTextAsync(Program.AppsettingsOverridesFile, JsonSerializer.Serialize(data));
            logger.LogInformation("New token saved");

            return newToken;
        }

        protected async Task<EventsResponse> GetEvents(string childId, string newToken)
        {
            var dateStart = DateTimeOffset.Now.AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dateEnd = DateTimeOffset.Now.AddDays(14).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/eventcalendar/v1/api/events?person_ids={childId}&begin_date={dateStart}&end_date={dateEnd}&expand=homework");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");
            req.Headers.TryAddWithoutValidation("X-Mes-Role", "parent");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var obj = (await resp.Content.ReadFromJsonAsync<EventsResponse>())!;

            foreach(var err in obj.errors.SelectMany(x => x.Value.Select(v => v.error_description)))
            {
                logger.LogWarning("Error: {Text}", err);
            }

            return obj;
        }
    }
}
