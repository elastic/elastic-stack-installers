... tracks design/development process for wave two of Windows installers.

Discussion topics

### Packaging
- [ ] Beat per MSI
- [ ] All beats in single MSI
- [ ] Establish service dependencies (TCP/IP, etc)

### Meta-Artifacts
- [ ] Hardcode list of all beats
- [ ] Pull list of beats from msi-bootstrapper-beats at run-time

### Artifacts
- [ ] Where from?
- [ ] Pull beat(s) build artifacts at MSI build time and create an offline package
- [ ] Pull beat(s) build artifacts at run-time and unpack/install

### Bitness
- [ ] Provide x86 version. Do we still care about x86/32bit variants?
- [ ] Provide x64 version. 

### UI - Provide per-beat configuration UI to:
- [ ] allow the beat to be installed as a service.
- [ ] allow the beat to connect to downstream component.
- [ ] run Kibana dashboard setup
- [ ] configure/enable available modules
- [ ] Direct user to the guide page for the beat to continue/modify/extend configuration.

### CM / Fleet integration
- [ ] ...

### Testing / CI
- [ ] ...
