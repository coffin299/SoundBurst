#Requires -RunAsAdministrator
#Requires -Version 5.1
pnputil /enum-drivers | Out-String | Write-Host
Write-Host ""
Write-Host "Find the SoundBurst / SoundBurstVad oemXX.inf published name above, then run:"
Write-Host "  pnputil /delete-driver oemXX.inf /uninstall /force"
