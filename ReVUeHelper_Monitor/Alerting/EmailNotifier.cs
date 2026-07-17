using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Configuration;

namespace ReVUeHelper_Monitor.Alerting;

/// <summary>
/// Sends alerts over SMTP. Uses the built-in System.Net.Mail client, which works with an
/// internal relay (anonymous or credentialed). Swap for MailKit later if TLS/auth needs grow.
/// </summary>
public sealed class EmailNotifier : INotifier
{
    private readonly EmailOptions _opt;
    private readonly ILogger<EmailNotifier> _log;

    public EmailNotifier(IOptions<MonitorOptions> opt, ILogger<EmailNotifier> log)
    {
        _opt = opt.Value.Email;
        _log = log;
    }

    public string Channel => "Email";
    public bool Enabled => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.SmtpHost) && _opt.To.Length > 0;

    public async Task SendAsync(string subject, string body, CancellationToken ct)
    {
        if (!Enabled)
        {
            _log.LogWarning("Email not sent: channel disabled or SmtpHost/To not configured.");
            return;
        }

        using var msg = new MailMessage { From = new MailAddress(_opt.From), Subject = subject, Body = body };
        foreach (var to in _opt.To) msg.To.Add(to);

        using var client = new SmtpClient(_opt.SmtpHost, _opt.SmtpPort) { EnableSsl = _opt.UseSsl };
        // Anonymous relay, matching the working ReVUeHelper_OSS/EmailService.cs (dfsmtpout).
        client.UseDefaultCredentials = false;

        await client.SendMailAsync(msg, ct);
        _log.LogInformation("Email sent to {To}: {Subject}", string.Join(", ", _opt.To), subject);
    }
}
