Write-Host "Installing Azure Storage Emulator..." -ForegroundColor Cyan
Write-Host "Downloading..."
$msiPath = "$($env:USERPROFILE)\MicrosoftAzureStorageEmulator.msi"
(New-Object Net.WebClient).DownloadFile('https://download.microsoft.com/download/F/3/8/F3857A38-D344-43B4-8E5B-2D03489909B9/MicrosoftAzureStorageEmulator.msi', $msiPath)
Write-Host "Installing..."
cmd /c start /wait msiexec /i "$msiPath" /q
del $msiPath
Write-Host "Installed Azure Storage Emulator" -ForegroundColor Green