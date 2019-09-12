# Windows MSI packages for Elastic Beats (POC)

### Feature set

```
GLOBAL OPTIONS:

  --help    Shows this help screen as well as help for specific commands


COMMANDS:

  show        Show available build targets
  discover    Discover Container Id's used for build and fetch commands
  fetch       Download and optionally unpack input artifacts
  build       Build Targets
```

- Discover branches, versions, aliases.
- Download and unpack artifacts.
- (WIP ~75%) Build Winlogbeat from downloaded artifact.

### Requirements

- .net core sdk v2.2.401 (https://dotnet.microsoft.com/download/thank-you/dotnet-sdk-2.2.401-windows-x64-installer)
- Wix Toolset v3.11.1 (https://github.com/wixtoolset/wix3/releases/download/wix3111rtm/wix311.exe)
or a newer v3.14: https://wixtoolset.org/releases/development/

### Running

Get help:

```
.\build 
```

### Knonwn issues
- License text
- Configuration yaml in the wrong place, service doesn't start yet