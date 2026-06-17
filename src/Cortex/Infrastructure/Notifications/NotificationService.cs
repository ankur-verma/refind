using System.Net;
using System.Net.Mail;
using Cortex.Infrastructure.Settings;
using Cortex.Modules.Auth.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Cortex.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IOptions<NotificationSettings> options,
        ILogger<NotificationService> logger)
    {
        _settings = options.Value;
        _logger = logger;

        if (!_settings.UseSimulatedNotifications && !string.IsNullOrWhiteSpace(_settings.Twilio.AccountSid))
        {
            TwilioClient.Init(_settings.Twilio.AccountSid, _settings.Twilio.AuthToken);
        }
    }

    public async Task SendWelcomeEmailAsync(string email, CancellationToken ct = default)
    {
        if (_settings.UseSimulatedNotifications)
        {
            _logger.LogInformation("[NOTIFICATION SIMULATOR] Welcome email triggered for {Email}.", email);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
            {
                Credentials = new NetworkCredential(_settings.Smtp.Username, _settings.Smtp.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.Smtp.FromEmail),
                Subject = "Welcome to Refind Cognitive Mind!",
                Body = "Thank you for registering. Your personal memory deck is ready to organize your bookmark lists, pictures, and videos.",
                IsBodyHtml = false
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage, ct);
            _logger.LogInformation("Successfully sent welcome email to {Email}.", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email} via SMTP.", email);
        }
    }

    public async Task SendVerificationEmailAsync(string email, string code, CancellationToken ct = default)
    {
        if (_settings.UseSimulatedNotifications)
        {
            _logger.LogInformation("[NOTIFICATION SIMULATOR] Verification email sent to {Email}. Code: {Code}", email, code);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
            {
                Credentials = new NetworkCredential(_settings.Smtp.Username, _settings.Smtp.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.Smtp.FromEmail),
                Subject = "Refind Security Verification Code",
                Body = $"Your 6-digit authentication security code is: {code}. Do not share this code.",
                IsBodyHtml = false
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage, ct);
            _logger.LogInformation("Sent verification email successfully to {Email}.", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}.", email);
        }
    }

    public async Task SendSmsOtpAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        if (_settings.UseSimulatedNotifications)
        {
            _logger.LogInformation("[NOTIFICATION SIMULATOR] SMS OTP triggered for {Phone}. Code: {Code}", phoneNumber, code);
            return;
        }

        try
        {
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(phoneNumber),
                from: new PhoneNumber(_settings.Twilio.FromPhoneNumber),
                body: $"Refind verification code: {code}. Expires in 5 minutes."
            );

            _logger.LogInformation("Sent SMS OTP successfully to {Phone}. Twilio SID: {Sid}", phoneNumber, message.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver SMS OTP to {Phone} via Twilio.", phoneNumber);
        }
    }
}
