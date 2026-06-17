namespace Cortex.Modules.Auth.Services;

public interface INotificationService
{
    Task SendWelcomeEmailAsync(string email, CancellationToken ct = default);
    Task SendVerificationEmailAsync(string email, string code, CancellationToken ct = default);
    Task SendSmsOtpAsync(string phoneNumber, string code, CancellationToken ct = default);
}
