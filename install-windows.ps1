# Installation script for Windows
Install-WindowsFeature -name Web-Server -IncludeManagementTools

New-Item \temp\ -ItemType Directory -Force
Set-Location \temp\

# The .NET Core Hosting Bundle
# https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-9.0
# https://dotnet.microsoft.com/permalink/dotnetcore-current-windows-runtime-bundle-installer
Invoke-WebRequest "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/9.0.1/dotnet-hosting-9.0.1-win.exe" -OutFile dotnet-hosting.exe -ProgressAction SilentlyContinue
.\dotnet-hosting.exe /quiet

Invoke-WebRequest "https://github.com/JanneMattila/webapp-network-tester/releases/download/v1.0.0/webappnetworktester-windows.zip" -OutFile webapp-network-tester.zip -ProgressAction SilentlyContinue
Expand-Archive webapp-network-tester.zip -DestinationPath \inetpub\wwwroot

$config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\webappnetworktester.exe"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
"@

Set-Content \inetpub\wwwroot\web.config -Value $config

# Force the IIS to restart
net stop was /y
net start w3svc

# New-NetFirewallRule `
#     -DisplayName "IIS" `
#     -LocalPort 80 `
#     -Action Allow `
#     -Profile 'Public' `
#     -Protocol TCP `
#     -Direction Inbound
