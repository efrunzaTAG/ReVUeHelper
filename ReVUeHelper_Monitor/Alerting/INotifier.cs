namespace ReVUeHelper_Monitor.Alerting;

/// <summary>A delivery channel (email, SMS, ...). Same contract for every channel.</summary>
public interface INotifier
{
    string Channel { get; }
    bool Enabled { get; }
    Task SendAsync(string subject, string body, CancellationToken ct);
}
