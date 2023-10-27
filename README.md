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

