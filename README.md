# Windows MSI packages for Elastic stack

### Feature set
- Discover branches, versions and aliases
- Fetch any beat from above
- Build any beat from downloaded archives in non-OSS variant
- Configuration and misc files installed into ProgramData/Elastic/{version}/Beats/{beat}/

### Get help:
```
$ .\build 
ElastiBuild v1.0.0
Copyright (c) 2019, Elastic.co

Global Flags:

  --help    Shows this help screen as well as help for specific commands

Available Commands:

  build       Build Targets
  discover    Discover Container Id's used for build and fetch commands
  fetch       Download and optionally unpack input artifacts
  show        Show available build targets
  ```

- Discover branches, versions, aliases.
- Download and unpack artifacts.
- (WIP ~95%) Build all from downloaded artifacts.

### Example: building 7.4 SNAPSHOT for winlogbeat, filebeat and functionbeat:
```
.\build.bat build --cid 7.4 --bitness x64 winlogbeat filebeat functionbeat
```

### Requirements

- Visual Studio 2019 v16.3 (with .Net Core v3.0, SDK v3.0.100)
- Wix Toolset v3.11.1 (https://github.com/wixtoolset/wix3/releases/download/wix3111rtm/wix311.exe)
or a newer v3.14: https://wixtoolset.org/releases/development/

### Known issues
- See #4
- Non-OSS only.
- Command `show` is disabled for now, refactoring usage of BullsEye.
