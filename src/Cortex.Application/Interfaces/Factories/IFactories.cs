namespace Cortex.Application.Interfaces.Factories;

/// <summary>
/// Factory Pattern: Creates AI processing pipelines based on content type.
/// </summary>
public interface IAIProcessorFactory
{
    IAIPipeline CreatePipeline(string pipelineType);
}

/// <summary>
/// Common interface for all AI processing pipelines.
/// </summary>
public interface IAIPipeline
{
    Task<object> ExecuteAsync(string input, CancellationToken ct = default);
}

/// <summary>
/// Factory Pattern: Creates the appropriate notification delivery strategy.
/// </summary>
public interface INotificationFactory
{
    INotificationSender CreateSender(NotificationChannel channel);
}

/// <summary>
/// Common interface for notification delivery (push, email, in-app).
/// </summary>
public interface INotificationSender
{
    Task SendAsync(string recipient, string title, string body, CancellationToken ct = default);
}

/// <summary>
/// Notification delivery channels.
/// </summary>
public enum NotificationChannel
{
    Push,
    Email,
    InApp
}
