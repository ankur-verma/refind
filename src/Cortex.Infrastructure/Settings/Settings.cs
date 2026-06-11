namespace Cortex.Infrastructure.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Cortex.API";
    public string Audience { get; set; } = "Cortex.Clients";
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
}

public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "cortex";
    public string Password { get; set; } = "cortex_dev";
    public string QueueName { get; set; } = "ai_extraction_queue";
    public string DeadLetterExchange { get; set; } = "cortex.ai.dlx";
    public string DeadLetterQueueName { get; set; } = "ai_extraction_dead_letter_queue";
    public int RetryDelaySeconds { get; set; } = 10;
}

public class GoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class MinioSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "cortex-content";
}

public class AIServiceSettings
{
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string CompletionModel { get; set; } = "gpt-4o-mini";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 60;
}
