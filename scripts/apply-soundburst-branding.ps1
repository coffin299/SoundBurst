#Requires -Version 5.1
<#
.SYNOPSIS
  Renames SysVAD friendly strings toward SoundBurst Speakers / Microphone.
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sysvad = Join-Path $root "driver\SoundBurstVad\sysvad"
if (-not (Test-Path $sysvad)) {
    throw "SysVAD not found. Run .\scripts\fetch-sysvad.ps1 first."
}

$replacements = [ordered]@{
    "Microsoft Virtual Audio Device (WDM) - Tablet Audio Sample" = "SoundBurst Virtual Audio Device"
    "Tablet Audio Sample" = "SoundBurst Virtual Audio"
    "Sysvad Tablet" = "SoundBurst"
    "Microphone Array" = "SoundBurst Microphone"
}

$files = Get-ChildItem -Path $sysvad -Recurse -Include *.inf,*.rc,*.h,*.cpp,*.c,*.mc
$count = 0
foreach ($file in $files) {
    $text = Get-Content -Raw -Path $file.FullName -ErrorAction SilentlyContinue
    if ($null -eq $text) { continue }
    $original = $text
    foreach ($k in $replacements.Keys) {
        $text = $text.Replace($k, $replacements[$k])
    }
    if ($text -ne $original) {
        Set-Content -Path $file.FullName -Value $text -NoNewline
        $count++
        Write-Host "Updated $($file.FullName)"
    }
}

# Prefer endpoint branding constants header for contributors
$header = Join-Path $root "driver\SoundBurstVad\src\loopback_notes.h"
Copy-Item $header (Join-Path $sysvad "soundburst_loopback_notes.h") -Force
Copy-Item (Join-Path $root "driver\SoundBurstVad\inf\soundburstvad.inf") `
    (Join-Path $sysvad "soundburstvad.inf") -Force

Write-Host "Branding pass complete ($count files touched)."
Write-Host "Manually set WaveRT render/capture friendly names to:"
Write-Host "  SoundBurst Speakers / SoundBurst Microphone"
Write-Host "Then build with Visual Studio + WDK (x64) and run test-sign-driver.ps1"
