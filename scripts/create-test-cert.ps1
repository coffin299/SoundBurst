#Requires -RunAsAdministrator
#Requires -Version 5.1
<#
.SYNOPSIS
  Creates a self-signed test certificate for SoundBurstVad (Test Mode only).
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "driver\SoundBurstVad\certs"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$subject = "CN=SoundBurst Test"
$existing = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $subject }
if ($existing) {
    Write-Host "Certificate already exists: $($existing[0].Thumbprint)"
    $cert = $existing[0]
}
else {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $subject `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(5)
    Write-Host "Created certificate: $($cert.Thumbprint)"
}

$cerPath = Join-Path $outDir "SoundBurstTest.cer"
$pfxPath = Join-Path $outDir "SoundBurstTest.pfx"
$password = ConvertTo-SecureString -String "SoundBurstDev" -Force -AsPlainText

Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password -Force | Out-Null

# Trust for local machine test signing workflows
Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null
Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\LocalMachine\TrustedPublisher" | Out-Null

Write-Host "Exported:"
Write-Host "  $cerPath"
Write-Host "  $pfxPath (password: SoundBurstDev)"
Write-Host "Installed into LocalMachine Root + TrustedPublisher"
