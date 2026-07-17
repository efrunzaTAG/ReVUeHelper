# ReVUeHelper_Monitor

A Windows Service that tails the ReVue application's `dbo.LogEvents` table and alerts on
watched logins (`Logger = 'WhoAmI'` + `Username` on a watchlist). Email fires on every
occurrence; an ntfy.sh push (iOS) is windowed, cooled down, and queued/summarized.

## How it works

- **Poll loop** (`Worker.cs`): reads new rows `WHERE LogEventId > watermark AND Logger = 'WhoAmI'
  AND Username IN (watchlist)` `WITH (NOLOCK)`, oldest first. Fast during business hours, sparse otherwise.
- **Watermark** (`state/watermark.json`): last processed `LogEventId`, persisted per row so a
  restart never re-alerts or misses. First run seeds to the current `MAX(LogEventId)` so history
  is not replayed.
- **Delivery rules** (`NotificationScheduler.cs`):
  - **Email** — every matching login, no cooldown, any time.
  - **ntfy push** — only inside `[WindowStart, WindowEnd]` (default 08:30–21:00); 3h cooldown;
    suppressed entirely on weekends (email-only); events seen outside the window or during cooldown
    are queued and released as **one summary push** when the window is open and the cooldown has
    elapsed. The queue (`state/pending.json`) is persisted, so an alert queued overnight survives a
    restart and still goes out at 08:30. Sends an unauthenticated POST to `{BaseUrl}/{Topic}` with
    `Title`/`Priority` headers; access control is the hard-to-guess topic name. Sound/repeat/Focus
    are configured on the phone.

## Configuration

- `appsettings.json` → `Monitor` section: connection string (Integrated Security — no credentials),
  poll cadence, SMTP settings, ntfy topic/window/cooldown, time zone.
- **Watched usernames are NOT in source control.** Resolution order:
  1. `secrets.json` in `StateDirectory` (default `C:\ProgramData\ReVUeHelper_Monitor\secrets.json`) —
     `{ "WatchUsers": [ "user1", "user2" ] }`. Copy from `secrets.sample.json`.
  2. Env var `REVUE_MONITOR_WATCH_USER` (comma-separated).
  3. `dotnet user-secrets` (`Monitor:WatchUsers`) — for local dev only.

## Run locally (this dev box)

```powershell
dotnet user-secrets set "Monitor:WatchUsers:0" "someuser"   # or create secrets.json
dotnet run --project ReVUeHelper_Monitor
```
Runs as a console app; Ctrl+C stops it.

## Publish (on the dev box)

```powershell
# Self-contained, single-file exe — no .NET needed on the server.
.\ReVUeHelper_Monitor\publish.ps1
# ...or publish AND copy to the server in one step (stop the service first if updating):
.\ReVUeHelper_Monitor\publish.ps1 -DestPath \\MYSERVER\c$\Services\ReVUeHelper_Monitor
```
Output folder needs: `ReVUeHelper_Monitor.exe`, `Microsoft.Data.SqlClient.SNI.dll` (native, required),
and `appsettings.json`.

## First-time install on the server (once)

```powershell
# 1. Copy the published folder to e.g. C:\Services\ReVUeHelper_Monitor
# 2. Create the state dir + secrets file (NOT in git)
New-Item -ItemType Directory -Force C:\ProgramData\ReVUeHelper_Monitor
'{ "WatchUsers": [ "someuser" ] }' | Set-Content C:\ProgramData\ReVUeHelper_Monitor\secrets.json -Encoding utf8
# 3. Install as a service (elevated PowerShell). Prompts for the account password.
.\install-service.ps1 -BinDir C:\Services\ReVUeHelper_Monitor -Account "AMHERST\you"
```

## Day-to-day operation

- **Start / Stop / Restart:** use **services.msc** (find "ReVUeHelper_Monitor"), or
  `sc.exe start|stop ReVUeHelper_Monitor`. (Pause is not supported — use Restart.)
- **Changed `appsettings.json` or `secrets.json`?** They're read at startup — just **Restart** the
  service. No republish.
- **Changed code?** Stop the service → re-run `publish.ps1 -DestPath ...` → Start the service.

### Service identity & avoiding account lockout

A service stores its logon password in the LSA secret store and only uses it at service **start**.
A running service is unaffected by a domain password change — the risk is only a restart afterwards,
where the SCM tries the stale password, gets a logon failure (event 1069/7000), and the service simply
**fails to start** (it does not retry in a tight loop). One stale start = one bad AD attempt, so lockout
is only a real risk if you reboot/start repeatedly with an out-of-date stored password.

Options, best first:
1. **gMSA** — `sc.exe create ... obj= "DOMAIN\gmsaName$" password= ""` (blank). AD auto-rotates the
   password; your account is never involved and can't be locked. Grant the gMSA read on ReVue.
2. **Dedicated service account** with password-never-expires; grant it read on ReVue.
3. **Your personal account** — fine to start with. After any domain password change, update the stored
   password and don't reboot with a stale one:
   ```
   sc.exe config ReVUeHelper_Monitor obj= "DOMAIN\you" password= "NEW_PASSWORD"
   ```

> If you use a machine environment variable for the watchlist instead of `secrets.json`, note that a
> Windows Service only sees a newly-added machine variable after a **reboot** (the SCM captures the
> system environment block at boot). `secrets.json` avoids that.
