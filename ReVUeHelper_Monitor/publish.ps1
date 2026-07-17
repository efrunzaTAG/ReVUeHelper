<#
.SYNOPSIS
  Publishes ReVUeHelper_Monitor as a self-contained, single-file Windows exe (no .NET needed on the target).

.EXAMPLE
  # Publish to the default .\publish folder:
  .\publish.ps1

.EXAMPLE
  # Publish AND copy the output to the server (must be stopped first if updating the exe):
  .\publish.ps1 -DestPath \\MYSERVER\c$\Services\ReVUeHelper_Monitor
#>
[CmdletBinding()]
param(
    # Output MUST be outside the project folder, or MSBuild re-globs its own content files.
    [string]$Output = "C:\Temp\ReVUeHelper_Monitor_publish",
    [string]$DestPath = ""
)

$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "ReVUeHelper_Monitor.csproj"

# Start from a clean output folder so stale/duplicate files can't accumulate.
if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }

Write-Host "Publishing $proj -> $Output" -ForegroundColor Cyan
dotnet publish $proj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o $Output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# Ship the install helper alongside the exe so the whole folder is self-contained.
Copy-Item (Join-Path $PSScriptRoot "install-service.ps1") $Output -Force

Write-Host "`nPublished files:" -ForegroundColor Green
Get-ChildItem $Output | Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}} | Format-Table -AutoSize

if ($DestPath) {
    Write-Host "Copying to $DestPath ..." -ForegroundColor Cyan
    Write-Host "  (If updating a running service, STOP it first so the .exe isn't locked.)" -ForegroundColor Yellow
    New-Item -ItemType Directory -Force $DestPath | Out-Null
    # Always replace the code (exe + native SNI dll) and refresh the install helper.
    Copy-Item "$Output\ReVUeHelper_Monitor.exe"          $DestPath -Force
    Copy-Item "$Output\Microsoft.Data.SqlClient.SNI.dll" $DestPath -Force
    Copy-Item "$Output\install-service.ps1"              $DestPath -Force
    # appsettings.json is server-owned config: copy only on first deploy, never overwrite your edits.
    $destSettings = Join-Path $DestPath "appsettings.json"
    if (-not (Test-Path $destSettings)) {
        Copy-Item "$Output\appsettings.json" $DestPath
        Write-Host "Copied appsettings.json (first deploy)." -ForegroundColor Green
    } else {
        Write-Host "Kept existing appsettings.json on target (edit it there, then restart)." -ForegroundColor Yellow
    }
    # secrets.json lives in C:\ProgramData\ReVUeHelper_Monitor, never copied.
    Write-Host "Done. Restart the service to pick up changes." -ForegroundColor Green
}
