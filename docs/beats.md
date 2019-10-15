# Beats Windows Installer packages

Beats is the platform for single-purpose data shippers. They send data from hundreds or thousands of machines and systems to Logstash or Elasticsearch.

### Current version of [ElastiBuild](elastibuild.md) supports these Beats:
  - [Auditbeat](https://www.elastic.co/products/beats/auditbeat)
  - [Filebeat](https://www.elastic.co/products/beats/Filebeat)
  - [Functionbeat](https://www.elastic.co/products/beats/Functionbeat)
  - [Heartbeat](https://www.elastic.co/products/beats/Heartbeat)
  - [Metricbeat](https://www.elastic.co/products/beats/Metricbeat)
  - [Packetbeat](https://www.elastic.co/products/beats/Packetbeat)
  - [Winlogbeat](https://www.elastic.co/products/beats/Winlogbeat)

### Usage



### Notes:

- Configuration files (.yml), Kibana dashboards, security and monitoring modules, etc are installed into `%ProgramData%\Elastic\{version}\Beats\{beat name}` directory.
- Beats that support running as Windows Service are registered as such. Service will **not** be started after installation finishes, because configuration file likely lacks proper host information for elasticsearch. In this version, user needs to manually edit the configuration file to point Beats to an elasticsearch cluster.
- When running as a Windows Service, Beats will create their `logs` and `data` directories in the location mentioned above.

### Known Issues:

- When running Beats from CLI (to register Kibana dashboards, or work with KeyStore for example), `path.home` and its related paths must be specified manually. This will be addressed in future versions.
- Current version of [ElastiBuild](elastibuild.md) supports building Non-OSS packages only.
