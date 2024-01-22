Pester test suite for testing Elastic Agent MSIs

Place a 8.10.4 and an 8.11.1 agent in a bin folder and run Invoke-Pester.

You will have to modify code if you want to run other versions.

It runs the MSI installer in various scenarios including Default, Standalone, and Fleet, all which pass different parameters to the MSI installer. It then tests the MSI exit code and other conditions to determine if the test passed.