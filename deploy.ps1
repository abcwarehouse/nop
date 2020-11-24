$appPoolName = "NopABC"

dotnet clean NopCommerce.sln
dotnet build NopCommerce.sln
dotnet clean NopCommerce.sln -c Release
dotnet build NopCommerce.sln -c Release

Stop-WebAppPool -Name $appPoolName | Out-Null
Write-Host 'App Pool Stopped.'
Start-Sleep -s 15
rm -Force -Recurse C:/NopABC/Plugins
dotnet publish -c Release ./Presentation/Nop.Web/Nop.Web.csproj --no-restore -o C:/NopABC
Start-WebAppPool -Name $appPoolName -ErrorAction 'SilentlyContinue' | Out-Null
Write-Host 'App Pool Started.'
