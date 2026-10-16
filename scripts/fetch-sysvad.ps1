#Requires -Version 5.1
<#
.SYNOPSIS
  Fetches Microsoft SysVAD audio sample into driver/SoundBurstVad/sysvad
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dest = Join-Path $root "driver\SoundBurstVad\sysvad"
$tmp = Join-Path $env:TEMP "soundburst-sysvad-$(Get-Random)"

Write-Host "Fetching Windows-driver-samples (audio/sysvad)..."
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
try {
    $zipUrl = "https://github.com/microsoft/Windows-driver-samples/archive/refs/heads/main.zip"
    $zipPath = Join-Path $tmp "samples.zip"
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
    Expand-Archive -Path $zipPath -DestinationPath $tmp -Force
    $src = Get-ChildItem $tmp -Directory | Where-Object { $_.Name -like "Windows-driver-samples-*" } |
        ForEach-Object { Join-Path $_.FullName "audio\sysvad" }
    if (-not (Test-Path $src)) {
        throw "sysvad folder not found in archive"
    }
    if (Test-Path $dest) {
        Remove-Item -Recurse -Force $dest
    }
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item -Recurse $src $dest
    Write-Host "SysVAD copied to $dest"
    Write-Host "Next: .\scripts\apply-soundburst-branding.ps1"
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
