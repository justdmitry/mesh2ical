using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

#pragma warning disable S1075 // URIs should not be hardcoded

namespace SchoolHelper.Mesh
{
    public class MeshExportService(ILogger<MeshExportService> logger, HttpClient httpClient, StateService stateService)
    {
        protected static readonly TimeSpan MaxTokenLifetime = TimeSpan.FromMinutes(20);

        public async Task<Child[]> GetFamily()
        {
            var token = await GetToken();

            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/family/web/v1/profile");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var family = await resp.Content.ReadFromJsonAsync<ProfileResponse>();

            logger.LogInformation("Got family with {Count} children", family?.children.Length);

            return family?.children ?? [];
        }

        public async Task<List<Lesson>> GetClasses(string personId, bool skipAdditionalSources)
        {
            ArgumentException.ThrowIfNullOrEmpty(personId);

            var token = await GetToken();

            var dateStart = DateTimeOffset.Now.AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dateEnd = DateTimeOffset.Now.AddDays(14).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/eventcalendar/v1/api/events?person_ids={personId}&begin_date={dateStart}&end_date={dateEnd}&expand=homework");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");
            req.Headers.TryAddWithoutValidation("X-Mes-Role", "parent");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var events = await resp.Content.ReadFromJsonAsync<EventsResponse>();

            if (events?.errors != null)
            {
                foreach (var err in events.errors.SelectMany(x => x.Value.Select(v => v.error_description)))
                {
                    logger.LogWarning("Error: {Text}", err);
                }
            }

            return events?.response?
                .Where(x => !skipAdditionalSources || x.source == EventsResponse.SourcePlanEx || x.source == EventsResponse.SourceOutOfPlanEx)
                .Select(x => new Lesson()
                {
                    Id = x.id,
                    Start = x.start_at,
                    End = x.finish_at,
                    Name = x.subject_name,
                    Location = x.room_number,
                    Homework = x.homework?.descriptions,
                    Replaced = x.replaced,
                })
                .ToList() ?? [];
        }

        public async Task<(long? ContractId, decimal? Balance)> GetBalance(string personId)
        {
            ArgumentException.ThrowIfNullOrEmpty(personId);

            var token = await GetToken();

            var param = Uri.EscapeDataString($"[{{\"personId\":\"{personId}\"}}]");
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/food/meals/v3/clients/balance?clientIds={param}");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");
            req.Headers.TryAddWithoutValidation("X-Mes-RoleId", "2");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var balances = await resp.Content.ReadFromJsonAsync<List<MealsBalanceResponseItem>>();
            var balance = balances?.FirstOrDefault();
            var contractId = balance?.contractId;
            var sum = balance?.balance / 100.00M;

            logger.LogInformation("Got {Count} balances for {PersonId}/{ContractId}, first is {Sum}", balances?.Count, balance?.clientId.personId, contractId, sum);

            return (contractId, sum);
        }

        public async Task<(decimal? OrderSum3Days, decimal? OrderSum14Days)> GetPreorderSummary(string personId)
        {
            ArgumentException.ThrowIfNullOrEmpty(personId);

            var token = await GetToken();

            var param = Uri.EscapeDataString($"{{\"personId\":\"{personId}\"}}");
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/food/meals/v3/orders/preorder/summary?clientId={param}");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");
            req.Headers.TryAddWithoutValidation("X-Mes-RoleId", "2");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var obj = await resp.Content.ReadFromJsonAsync<MealsPreorderSummaryResponse>();
            var sum3 = obj?.orderSum3Days / 100.00M;
            var sum14 = obj?.orderSum14Days / 100.00M;

            logger.LogInformation("Got preorder summary for {PersonId}: {Sum1} for 3 days, {Sum2} for 14 days", personId, sum3, sum14);

            return (sum3, sum14);
        }

        public async IAsyncEnumerable<MealInfo> GetMeals(int contractId, DateOnly date)
        {
            var token = await GetToken();

            var meals = await GetMealsV2(contractId, date, token);
            foreach (var complex in meals.SelectMany(x => x.items).Where(x => x.complex != null).Select(x => x.complex))
            {
                yield return new MealInfo
                {
                    Name = complex.name,
                    Content = string.Join(Environment.NewLine, complex.items.Select(x => x.name)),
                };
            }
        }

        public async Task<(decimal Preordered, decimal Purchased, decimal Other)> GetMealsSummary(string personId, DateOnly date)
        {
            ArgumentException.ThrowIfNullOrEmpty(personId);

            var token = await GetToken();

            var param = Uri.EscapeDataString($"{{\"personId\":\"{personId}\"}}");
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/food/meals/v3/orders?clientId={param}&from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}&limit=50");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");
            req.Headers.TryAddWithoutValidation("X-Mes-RoleId", "2");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var meals = await resp.Content.ReadFromJsonAsync<MealsV3OrdersResponseItem>();

            var totals = meals.orders
                .Aggregate<MealsV3OrdersResponseItem.Order, (decimal, decimal, decimal)>(
                    (0, 0, 0),
                    (x, y) =>
                    {
                        switch (y.orderType)
                        {
                            case 3:
                                x.Item1 += y.totalPrice / 100.00M;
                                break;

                            case 2:
                                x.Item2 += y.totalPrice / 100.00M;
                                break;

                            default:
                                x.Item3 += y.totalPrice / 100.00M;
                                break;
                        }
                        return x;
                    });

            logger.LogInformation("Got meals summary for {PersonId}/{Date}: {Sum1} for preorders, {Sum2} for purchased, {Sum3} for other.", personId, date, totals.Item1, totals.Item2, totals.Item3);

            return totals;
        }

        protected async Task<string> GetToken()
        {
            var state = await stateService.Load();

            var oldToken = state.MeshToken;

            if (string.IsNullOrEmpty(oldToken))
            {
                throw new InvalidOperationException("No token configured");
            }
            else if (GetTokenExpiration(oldToken) > DateTimeOffset.UtcNow.AddHours(7))
            {
                logger.LogDebug("Token is fresh enough, no need to update.");
                return oldToken;
            }

            var content = new Dictionary<string, string?>()
            {
                ["refresh_token"] = state.MeshRefreshToken,
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "https://school.mos.ru/v3/token/refresh");
            req.Content = new FormUrlEncodedContent(content);

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var newToken = await resp.Content.ReadFromJsonAsync<TokenRefreshResponse>();

            await stateService.Save(
                s =>
                {
                    s.MeshToken = newToken.access_token;
                    s.MeshRefreshToken = newToken.refresh_token;
                });

            logger.LogInformation(
                "Token updated, expires {Expires}, refresh token expires {Expires2}",
                GetTokenExpiration(newToken.access_token),
                GetTokenExpiration(newToken.refresh_token));

            return newToken.access_token;
        }

        protected async Task<List<MealsV2OrdersResponseItem>> GetMealsV2(int contractId, DateOnly date, string token)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://school.mos.ru/api/meals/v2/orders?contractId={contractId}&from={date:yyyy-MM-dd}T00:00:01&to={date:yyyy-MM-dd}T23:59:00");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-mes-subsystem", "familyweb");

            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            return (await resp.Content.ReadFromJsonAsync<List<MealsV2OrdersResponseItem>>())!;
        }

        private static DateTimeOffset GetTokenExpiration(string token)
        {
            var payloadBase64 = token.Split('.')[1];
            var payloadText = payloadBase64.Replace('_', '/').Replace('-', '+').PadRight(4 * ((payloadBase64.Length + 3) / 4), '=');
            var payloadJson = JsonNode.Parse(Convert.FromBase64String(payloadText));
            var expires = payloadJson["exp"].GetValue<int>();
            return DateTimeOffset.FromUnixTimeSeconds(expires);
        }
    }
}
