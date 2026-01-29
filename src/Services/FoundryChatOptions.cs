namespace ZavaStorefront.Services
{
    public sealed class FoundryChatOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "2024-05-01-preview";
    }
}
