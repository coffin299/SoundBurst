#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes the app with Velopack and optionally uploads to GitHub Releases.
.PARAMETER Version
  SemVer for this release (e.g. 0.1.0)
.PARAMETER Repo
  GitHub owner/name (e.g. coffi/SoundBurst). Used by vpk upload.
.PARAMETER Upload
  If set, runs `vpk upload github`
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Repo = "",
    [switch]$Upload
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" +
            [System.Environment]::GetEnvironmentVariable("Path","User")

Write-Host "Publishing app..."
dotnet publish src/SoundBurst.App/SoundBurst.App.csproj -c Release -r win-x64 --self-contained true -o "$root\artifacts\publish"

Write-Host "Ensuring vpk tool..."
dotnet tool update -g vpk 2>$null
if ($LASTEXITCODE -ne 0) {
    dotnet tool install -g vpk
}

$packDir = Join-Path $root "artifacts\velopack"
New-Item -ItemType Directory -Force -Path $packDir | Out-Null

Write-Host "Packing with Velopack $Version..."
vpk pack `
    --packId SoundBurst `
    --packVersion $Version `
    --packDir "$root\artifacts\publish" `
    --mainExe SoundBurst.exe `
    --outputDir $packDir `
    --packTitle "SoundBurst" `
    --runtime win-x64

Write-Host "Packages in $packDir"
Write-Host "NOTE: Driver packages are NOT included. Attach driver assets separately to the GitHub Release."

if ($Upload) {
    if ([string]::IsNullOrWhiteSpace($Repo)) {
        throw "Specify -Repo owner/name when using -Upload"
    }
    vpk upload github --repo $Repo --outputDir $packDir --publish --releaseName "SoundBurst $Version"
}

Write-Host "Done."
