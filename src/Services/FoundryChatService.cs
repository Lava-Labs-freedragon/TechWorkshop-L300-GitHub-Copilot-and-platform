using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ZavaStorefront.Services
{
    public class FoundryChatService
    {
        private readonly HttpClient _httpClient;
        private readonly FoundryChatOptions _options;
        private readonly ILogger<FoundryChatService> _logger;

        public FoundryChatService(HttpClient httpClient, IOptions<FoundryChatOptions> options, ILogger<FoundryChatService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> SendAsync(string message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
                string.IsNullOrWhiteSpace(_options.ApiKey) ||
                string.IsNullOrWhiteSpace(_options.DeploymentName))
            {
                throw new InvalidOperationException("Foundry configuration is missing.");
            }

            var endpoint = _options.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/openai/deployments/{_options.DeploymentName}/chat/completions?api-version={_options.ApiVersion}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", _options.ApiKey);

            var payload = new
            {
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                temperature = 0.2,
                max_tokens = 256
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Foundry request failed with status {StatusCode}: {Content}", response.StatusCode, content);
                throw new InvalidOperationException("Foundry request failed.");
            }

            using var doc = JsonDocument.Parse(content);
            var reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return reply ?? string.Empty;
        }
    }
}
