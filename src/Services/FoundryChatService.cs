using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace ZavaStorefront.Services
{
    public class FoundryChatService
    {
        private readonly HttpClient _httpClient;
        private readonly FoundryChatOptions _options;
        private readonly ILogger<FoundryChatService> _logger;
        private readonly TokenCredential _credential;

        public FoundryChatService(HttpClient httpClient, IOptions<FoundryChatOptions> options, ILogger<FoundryChatService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
            _credential = new DefaultAzureCredential();
        }

        public async Task<string> SendAsync(string message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
                string.IsNullOrWhiteSpace(_options.DeploymentName))
            {
                throw new InvalidOperationException("Foundry configuration is missing.");
            }

            var endpoint = _options.Endpoint.Trim();
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
            {
                throw new InvalidOperationException("Foundry endpoint is invalid.");
            }

            var usesModelsEndpoint = endpointUri.AbsolutePath.Contains("/models/", StringComparison.OrdinalIgnoreCase)
                || endpointUri.AbsolutePath.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase);

            var requestUri = usesModelsEndpoint
                ? BuildEndpointUri(endpointUri, _options.ApiVersion)
                : new Uri($"{endpoint.TrimEnd('/')}/openai/deployments/{_options.DeploymentName}/chat/completions?api-version={_options.ApiVersion}");

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var token = await _credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }),
                cancellationToken);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

            object payload = usesModelsEndpoint
                ? new
                {
                    model = _options.DeploymentName,
                    messages = new[]
                    {
                        new { role = "user", content = message }
                    },
                    temperature = 0.2,
                    max_tokens = 256
                }
                : new
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

        private static Uri BuildEndpointUri(Uri endpointUri, string apiVersion)
        {
            if (!string.IsNullOrWhiteSpace(endpointUri.Query))
            {
                return endpointUri;
            }

            return new Uri($"{endpointUri.AbsoluteUri}?api-version={apiVersion}");
        }
    }
}
