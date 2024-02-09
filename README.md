# Welcome to the Elastic Stack Installers!

[![Build status](https://badge.buildkite.com/5e93d7c12dcedbceb8cb3e7935f17f94e9803543aa1d464e26.svg)](https://buildkite.com/elastic/elastic-stack-installers)

This repository contains [ElastiBuild](https://github.com/elastic/elastic-stack-installers/wiki/ElastiBuild) and [Beat Package Compiler](https://github.com/elastic/elastic-stack-installers/wiki/Beat-Package-Compiler). See [Wiki](https://github.com/elastic/elastic-stack-installers/wiki) for more information.

## Reporting Problems
To report any problems encountered during installation, or to request features, please open an [issue on GitHub](https://github.com/elastic/elastic-stack-installers/issues)  and attach the MSI installation log if applicable. 

For other questions of comments pleas refer to [Elastic Forums](https://discuss.elastic.co/tags/windows-installer). Please *tag* your question with `windows-installer` (singlular).

## Capturing Logs:
```
msiexec /i "<full path to msi file>" /l!*vx "<full path to log file to be created>"
```

Please *attach* log file to the issue you create and provide as much information about your environment as you can.

For other general questions and comments, please use the [Elastic discussion forum](https://discuss.elastic.co/).

### Building From Source

See [ElastiBuild](https://github.com/elastic/elastic-stack-installers/wiki/ElastiBuild)

---

**NOTE**: *Building from source should only be done for development purposes. Only the officially distributed and signed Elastic Stack Installers should be used in production. Using unofficial Elastic Stack Installers is not supported.*

---

## Bumping version

### After a patch release

Update version in `Directory.Build.props` in the branch for the related minor version (ex: https://github.com/elastic/elastic-stack-installers/pull/183).

### After a minor release

1. Create a branch for the next minor release from the main branch
2. Update the main branch:
    - Bump version in `Directory.Build.props`
    - Update `catalog-info.yaml`:
      - Add a new daily schedule for the new minor branch
      - Remove the daily schedule for the previous minor branch
    ex: https://github.com/elastic/elastic-stack-installers/pull/156 and https://github.com/elastic/elastic-stack-installers/pull/172

---
## Installing to a custom location
The default target folder for the MSI is typically something like `c:\Program Files\Elastic\Beats\<version>\<beat>` eg. `c:\Program Files\Elastic\Beats\8.12.0\winlogbeat`.

Starting version 8.13 It's also possible to override the installation folder by executing the MSI from command line as such:
`msiexec /i "<full path to msi file>" INSTALLDIR="<path of custom folder>"`

Few important notes:
- For security reasons, it's recommended to keep the installation folder "Program Files"
- An empty folder will always be created at the default location `...\Beats\<version>\<beat>` to to a known limitation as explained [here](https://github.com/oleg-shilo/wixsharp/issues/790).
- Custom installation folder is not applicable for Agent MSI (only beats)

---
## Agent

In case of problems during install / uninstall of agent, please refer to the [Capturing Logs](https://github.com/elastic/elastic-stack-installers/blob/agent_support/README.md#capturing-logs) section which will enable troubleshooting.

### Install
During the install flow, The MSI installer will unpack the contents of the MSI to a temp folder and then will call the `elastic-agent install` in order to:
1. copy the files to the final destination at `c:\Program Files\Elastic\Agent`
2. register the agent as a windows service
3. enroll the agent into fleet

In order to complete step 3 above, the MSI installer shall receive command line arguments, passed with INSTALLARGS command line switch followed by `"`, for example:
```
elastic-agent.msi INSTALLARGS="--url=<fleet_url_with_port> --enrollment-token=<token>"
```

Note that the MSI will call the `elastic-agent install` command with `-f` (force) to avoid user interaction.

### Uninstall
Similarly to the install flow (described above), the MSI will call the `elastic-agent uninstall` command, and it's possible to pass arguments using `INSTALLARGS`. One common use case is uninstalling an agent which has tamper protection enabled.

### Upgrade
The Agent MSI doesn't support upgrade. Since the agents are fleet managed, upgrades shall be done using fleet (UI / API).

