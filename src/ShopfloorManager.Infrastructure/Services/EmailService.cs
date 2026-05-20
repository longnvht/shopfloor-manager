using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Infrastructure.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = config["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("Email:Host chưa cấu hình — bỏ qua gửi email tới {To}", to);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(config["Email:From"] ?? "noreply@shopfloor.local"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(host,
            int.Parse(config["Email:Port"] ?? "587"),
            SecureSocketOptions.StartTlsWhenAvailable, ct);

        var user = config["Email:User"];
        if (!string.IsNullOrWhiteSpace(user))
            await client.AuthenticateAsync(user, config["Email:Password"], ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
