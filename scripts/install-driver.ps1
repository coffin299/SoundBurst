#Requires -RunAsAdministrator
#Requires -Version 5.1
<#
.SYNOPSIS
  Installs SoundBurstVad via pnputil from a built+signed driver folder.
#>
param(
    [Parameter(Mandatory = $false)]
    [string]$DriverDir = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($DriverDir)) {
    $DriverDir = Join-Path $root "driver\SoundBurstVad\out\x64"
}

$inf = Get-ChildItem $DriverDir -Filter *.inf -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $inf) {
    throw "No INF in $DriverDir"
}

Write-Host "Installing $($inf.FullName)"
pnputil /add-driver $inf.FullName /install
if ($LASTEXITCODE -ne 0) {
    throw "pnputil failed ($LASTEXITCODE)"
}

Write-Host "Driver package submitted. Check Device Manager for 'SoundBurst' devices."
Write-Host "Then select 'SoundBurst Speakers' as app output and 'SoundBurst Microphone' in Discord."
