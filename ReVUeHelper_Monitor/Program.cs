using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReVUeHelper_Monitor;
using ReVUeHelper_Monitor.Alerting;
using ReVUeHelper_Monitor.Configuration;
using ReVUeHelper_Monitor.Data;
using ReVUeHelper_Monitor.State;

var builder = Host.CreateApplicationBuilder(args);

// Run as a Windows Service when installed via sc.exe; runs as a console app when launched directly (F5).
// AddWindowsService also routes logs to the Windows Event Log when running under the SCM.
builder.Services.AddWindowsService(o => o.ServiceName = "ReVUeHelper_Monitor");

builder.Services.Configure<MonitorOptions>(builder.Configuration.GetSection(MonitorOptions.SectionName));

builder.Services.AddSingleton<MonitorClock>();
builder.Services.AddSingleton<StateStore>();
builder.Services.AddSingleton<SecretsProvider>();
builder.Services.AddSingleton<LogEventReader>();
builder.Services.AddSingleton<EmailNotifier>();
builder.Services.AddSingleton<NtfyNotifier>();
builder.Services.AddSingleton<NotificationScheduler>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
