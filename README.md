# Windows MSI packages for Elastic Beats (POC)

### Feature set

- Downloads and unpacks winlogbeat-7.3.0-windows-x86_64.zip (hardcoded).
- Builds MSI package, install as-a-service not supported yet.

### Requirements

- .net core sdk v2.2.401 (https://dotnet.microsoft.com/download/thank-you/dotnet-sdk-2.2.401-windows-x64-installer)
- Wix Toolset v3.11.1 (https://github.com/wixtoolset/wix3/releases/download/wix3111rtm/wix311.exe)

### Running

```
.\build winlogbeat
```

Run `.\build -t` to list available targets.

### Knonwn issues
- Lots, it's a POC