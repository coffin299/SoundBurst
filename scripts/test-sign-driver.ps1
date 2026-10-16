#Requires -Version 5.1
<#
.SYNOPSIS
  Test-signs SoundBurstVad.sys and generates/signs a catalog file.
.PARAMETER DriverDir
  Folder containing SoundBurstVad.sys (and INF).
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

$sys = Join-Path $DriverDir "SoundBurstVad.sys"
if (-not (Test-Path $sys)) {
    # Also accept TabletAudioSample.sys before rename
    $alt = Get-ChildItem $DriverDir -Filter *.sys -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $alt) {
        throw "No .sys found in $DriverDir. Build the driver with WDK first."
    }
    $sys = $alt.FullName
}

$kitRoots = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
    "${env:ProgramFiles}\Windows Kits\10\bin"
)
$signtool = Get-ChildItem -Path $kitRoots -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "\\x64\\" } |
    Select-Object -First 1
$inf2cat = Get-ChildItem -Path $kitRoots -Recurse -Filter Inf2Cat.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "\\x64\\" } |
    Select-Object -First 1

if (-not $signtool) {
    throw "signtool.exe not found. Install Windows SDK / WDK."
}

$cer = Join-Path $root "driver\SoundBurstVad\certs\SoundBurstTest.cer"
$pfx = Join-Path $root "driver\SoundBurstVad\certs\SoundBurstTest.pfx"
if (-not (Test-Path $pfx)) {
    throw "Test cert missing. Run create-test-cert.ps1 as Administrator first."
}

Write-Host "Signing $sys"
& $signtool.FullName sign /fd SHA256 /f $pfx /p "SoundBurstDev" /v $sys
if ($LASTEXITCODE -ne 0) { throw "signtool failed for .sys" }

$inf = Get-ChildItem $DriverDir -Filter *.inf | Select-Object -First 1
if ($inf -and $inf2cat) {
    Write-Host "Generating catalog via Inf2Cat..."
    & $inf2cat.FullName /driver:$DriverDir /os:10_X64
    $cat = Get-ChildItem $DriverDir -Filter *.cat | Select-Object -First 1
    if ($cat) {
        & $signtool.FullName sign /fd SHA256 /f $pfx /p "SoundBurstDev" /v $cat.FullName
    }
}

Write-Host "Test-sign complete."
