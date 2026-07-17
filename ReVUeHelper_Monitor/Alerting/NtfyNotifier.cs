using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Configuration;

namespace ReVUeHelper_Monitor.Alerting;

/// <summary>
/// Pushes alerts to an ntfy.sh topic (a dedicated iOS app, manageable via a Focus mode).
/// A simple unauthenticated POST: the message is the request body; "Title" and "Priority" ride
/// as headers. Access control is just the hard-to-guess topic name (accepted risk).
/// All sound / repeat / Focus behaviour is configured on the phone, not here.
/// </summary>
public sealed class NtfyNotifier : INotifier
{
    // Shared client: this is a singleton service sending at most a few requests per hour.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    private readonly NtfyOptions _opt;
    private readonly ILogger<NtfyNotifier> _log;

    public NtfyNotifier(IOptions<MonitorOptions> opt, ILogger<NtfyNotifier> log)
    {
        _opt = opt.Value.Ntfy;
        _log = log;
    }

    public string Channel => "Ntfy";
    public bool Enabled => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.Topic);

    public async Task SendAsync(string subject, string body, CancellationToken ct)
    {
        if (!Enabled)
        {
            _log.LogWarning("ntfy not sent: channel disabled or Topic not configured.");
            return;
        }

        var url = $"{_opt.BaseUrl.TrimEnd('/')}/{_opt.Topic}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain")
        };
        // ntfy reads Title/Priority from headers (must be ASCII-safe).
        req.Headers.TryAddWithoutValidation("Title", string.IsNullOrWhiteSpace(subject) ? _opt.Title : subject);
        req.Headers.TryAddWithoutValidation("Priority", _opt.Priority.ToString());

        using var resp = await Http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var detail = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"ntfy POST {url} returned {(int)resp.StatusCode} {resp.ReasonPhrase}: {detail}");
        }

        _log.LogInformation("ntfy push sent to topic '{Topic}' (priority {Priority}).", _opt.Topic, _opt.Priority);
    }
}
