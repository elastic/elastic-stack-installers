# Welcome to the Elastic Stack Installers wiki!

This repository contains [ElastiBuild](https://github.com/elastic/elastic-stack-installers/wiki/ElastiBuild) and [Beat Package Compiler](https://github.com/elastic/elastic-stack-installers/wiki/Beat-Package-Compiler).

## Reporting Problems
To report any problems encountered during installation, or to request features, please open an [issue on GitHub](https://github.com/elastic/elastic-stack-installers/issues)  and attach the MSI installation log if applicable. 

For other questions of comments pleas refer to [Elastic Forums](https://discuss.elastic.co/tags/windows-installer). Please *tag* your question with `windows-installer` (singlular).

## Capturing Logs:
```msiexec /l!*vx "<full path to log file to be created>" /i "<full path to msi file>"```

Please *attach* log file to the issue you create and provide as much information about your environment as you can.

For other general questions and comments, please use the [Elastic discussion forum](https://discuss.elastic.co/).

### Building From Source
---

**NOTE**: *Building from source should only be done for development purposes. Only the officially distributed and signed Elastic Stack Installers should be used in production. Using unofficial Elastic Stack Installers is not supported.*

---
