namespace Cortex.Infrastructure.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Cortex.API";
    public string Audience { get; set; } = "Cortex.Clients";
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class AuthSettings
{
    public bool RequireOtpVerification { get; set; } = true;
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
    public string ExtractionQueueName { get; set; } = "ai_extraction_queue";
    public string BehaviorQueueName { get; set; } = "behavior_events_queue";
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
    public string ActiveProvider { get; set; } = "Gemini"; // "Gemini" or "LocalOllama"
    
    public ProviderSettings Gemini { get; set; } = new();
    public OllamaProviderSettings LocalOllama { get; set; } = new();

    // Keep these for backward compatibility during transition if needed, or remove them
    // But since we are updating appsettings, we can just remove them and use the nested ones.
}

public class ProviderSettings
{
    public string EndpointUrl { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string CompletionModel { get; set; } = string.Empty;
    public string OpenAIApiKey { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 60;
}

public class OllamaProviderSettings : ProviderSettings
{
    public string VisionModel { get; set; } = "llava";
}

public class NotificationSettings
{
    public bool UseSimulatedNotifications { get; set; } = true;
    public SmtpSettings Smtp { get; set; } = new();
    public TwilioSettings Twilio { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@refind.ai";
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string VerifyServiceSid { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
}

