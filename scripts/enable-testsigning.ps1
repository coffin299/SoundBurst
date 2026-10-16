#Requires -RunAsAdministrator
#Requires -Version 5.1
<#
.SYNOPSIS
  Enables Windows test signing (requires reboot). May need Secure Boot disabled.
#>
$ErrorActionPreference = "Stop"
Write-Host "Enabling TESTSIGNING..."
try {
    bcdedit /set testsigning on
    if ($LASTEXITCODE -ne 0) {
        throw "bcdedit failed with exit $LASTEXITCODE"
    }
    Write-Host "TESTSIGNING is ON. Reboot required."
    Write-Host "If this failed with Secure Boot policy error, disable Secure Boot in UEFI, then re-run."
}
catch {
    Write-Error $_
    exit 1
}
