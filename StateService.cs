namespace SchoolHelper
{
    using System.Text.Json;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class StateService(ILogger<StateService> logger, IConfiguration configuration)
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);

        private readonly JsonSerializerOptions jsonOptions = JsonSerializerOptions.Web;

        private readonly string stateFilePath = Path.GetFullPath(configuration["StateFilePath"] ?? "state.json");

        public async Task<State> Load()
        {
            if (!File.Exists(stateFilePath))
            {
                logger.LogDebug("File {file} does not exist.", stateFilePath);
                return new();
            }

            var text = File.ReadAllText(stateFilePath);

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogDebug("File {file} is empty.", stateFilePath);
                return new();
            }

            logger.LogDebug("Loading {file}", stateFilePath);
            return JsonSerializer.Deserialize<State>(text, jsonOptions) ?? new();
        }

        public async Task Save(Action<State> updates)
        {
            await semaphore.WaitAsync();
            try
            {
                var state = await Load();
                updates.Invoke(state);
                using var file = File.Open(stateFilePath, FileMode.Create);
                await JsonSerializer.SerializeAsync(file, state, jsonOptions);
                logger.LogDebug("Saved {file}", stateFilePath);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
