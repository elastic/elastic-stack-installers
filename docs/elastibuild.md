# ElastiBuild - Windows Installer package compiler

### Feature set
- Discover branches, versions and aliases
- Fetch any beat from above
- Build any beat from downloaded archives in non-OSS variant
- Configuration and misc files installed into ProgramData/Elastic/{version}/Beats/{beat}/

### Get help:
```
C:\src\stack-elastic-installers (master)
$ ./build
ElastiBuild v1.0.0
Copyright (c) 2019, https://elastic.co

Global Flags:

  --help    Shows this help screen as well as help for specific commands


Available Commands:

  build       Build one or more installers
  clean       Clean downloaded .zip, temporary and output files
  discover    Discover Container Id's used for build and fetch commands
  fetch       Download and optionally unpack packages
  show        Show products that we can build
```

Help is available for each command as well:

```
C:\src\stack-elastic-installers (master)
$ ./build discover
ElastiBuild v1.0.0
Copyright (c) 2019, https://elastic.co

Global Flags:

  --help    Shows this help screen as well as help for specific commands

DISCOVER Flags:

  --cid               Container Id. One of: Branch, eg. 6.8, 7.x; Version, eg. 6.8.3-SNAPSHOT, 7.4.0; Alias, eg. 6.8,
                      7.x-SNAPSHOT;
  --oss               (Default: false) Show OSS artifacts
  --bitness           (Default: x64) Show artifacts of specific bitness: x86, x64
  PRODUCT (pos. 0)    Required. [PRODUCT [PRODUCT [...]]]

Usage Examples:

Discover branches, versions and aliases:
  ./build discover all

Discover available Winlogbeat packages for alias 6.8:
  ./build discover --cid 6.8 winlogbeat
```

### Example: building x64 winlogbeat, filebeat and functionbeat from 8.0-SNAPSHOT:
    .\build.bat build --cid 8.0-SNAPSHOT winlogbeat filebeat functionbeat

### Requirements
- .Net Core v3.0, SDK v3.0.100
- Full Framework >= 4.5 ([WixSharp](https://github.com/oleg-shilo/wixsharp) dependency)

### Known issues
- See [#4](https://github.com/elastic/stack-elastic-installers/issues/4)
- Non-OSS only.

### ToDo's:
- See [#3](https://github.com/elastic/stack-elastic-installers/issues/3)
