<#
.SYNOPSIS
  One-time install of ReVUeHelper_Monitor as a Windows Service. Run this ON THE SERVER, in an
  elevated (Run as Administrator) PowerShell. After this, use services.msc to Start/Stop/Restart.

.EXAMPLE
  .\install-service.ps1 -BinDir C:\Services\ReVUeHelper_Monitor -Account "AMHERST\efrunza"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$BinDir,
    [Parameter(Mandatory)] [string]$Account,
    [string]$ServiceName = "ReVUeHelper_Monitor"
)

$ErrorActionPreference = "Stop"
$exe = Join-Path $BinDir "ReVUeHelper_Monitor.exe"
if (-not (Test-Path $exe)) { throw "Not found: $exe (copy the published folder here first)." }

# Prompt for the account password securely (used only to register the service logon).
$sec = Read-Host "Password for $Account" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
$pwd  = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)

Write-Host "Creating service '$ServiceName'..." -ForegroundColor Cyan
sc.exe create $ServiceName binPath= "`"$exe`"" start= auto obj= "$Account" password= "$pwd" | Out-Null
sc.exe description $ServiceName "Monitors ReVue WhoAmI logins; alerts via email + ntfy." | Out-Null
# Auto-restart on crash (does not loop on a bad-password logon failure).
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Host "Starting..." -ForegroundColor Cyan
sc.exe start $ServiceName | Out-Null
Start-Sleep -Seconds 2
sc.exe query $ServiceName

Write-Host "`nInstalled. From now on use services.msc (or: sc.exe stop/start $ServiceName)." -ForegroundColor Green
