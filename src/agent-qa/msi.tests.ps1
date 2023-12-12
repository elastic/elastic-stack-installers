param (
    $PathToEarlyMSI,
    $PathToLatestMSI
)

###
# The 3 agent modes (default, standalone, fleet) are each a test case for the tests
# The health function represents how we determine if the agent is healthy in that mode
# and is frequently invoked after installing the agent.

# Test cases have to be generated before discovery so nothing in this section is usable in tests
BeforeDiscovery {
    Import-Module (Join-Path $PSScriptRoot "helpers.ps1") -Force -Verbose:$False

    $Testcases = @(
        @{ 
            # Agent mode
            Mode              = "Default"; 

            # The checks to run to assertain whether an agent running under this mode is healthy
            HealthFunction    = {
                Is-AgentInstallCachePresent | Should -BeTrue
                Is-AgentUninstallKeyPresent | Should -BeTrue
                Is-AgentBinaryPresent | Should -BeFalse
                Is-AgentServicePresent | Should -BeFalse
            }

            # The parameters to use to get a agent into this mode
            MSIInstallParameters = @{
                Flags    = '/norestart'
                LogToDir = Get-LogDir
            }
            MSIUninstallParameters = @{
                LogToDir = Get-LogDir
            }
        }
        @{ 
            Mode              = "Standalone"; 
            HealthFunction    = {
                if (Is-AgentInstallCachePresent) {
                    write-warning "Agent Install Cache Program Files/Elastic/Beats is still present with $(Get-AgentInstallCacheCount) entries"
                }
                #Is-AgentInstallCachePresent | Should -BeFalse
                Is-AgentUninstallKeyPresent | Should -BeTrue
                Is-AgentBinaryPresent | Should -BeTrue
                Is-AgentServicePresent | Should -BeTrue
                Is-AgentServiceRunning -Resolve | Should -BeTrue # Start the service if it's not running and expect it to remain running
                Has-AgentLogged | Should -BeTrue
                Has-AgentStandaloneLog | Should -BeTrue
            }
            MSIInstallParameters = @{
                Flags    = '/norestart INSTALLARGS="-v"'
                LogToDir = Get-LogDir
            }
            MSIUninstallParameters = @{
                LogToDir = Get-LogDir
            }
        } 
        @{ 
            Mode              = "Fleet"; 
            HealthFunction    = {
                if (Is-AgentInstallCachePresent) {
                    write-warning "Agent Install Cache Program Files/Elastic/Beats is still present with $(Get-AgentInstallCacheCount) entries"
                }
                #Is-AgentInstallCachePresent | Should -BeFalse
                Is-AgentUninstallKeyPresent | Should -BeTrue
                Is-AgentBinaryPresent | Should -BeTrue
                Is-AgentServicePresent | Should -BeTrue

                # Start the service but don't expect it to be running as our fleet config causes agent to stop right away 
                Is-AgentServiceRunning -Resolve -WaitForExit

                Has-AgentLogged | Should -BeTrue
                Has-AgentFleetEnrollmentAttempt | Should -BeTrue
            }
            MSIInstallParameters = @{
                Flags    = '/norestart INSTALLARGS="--delay-enroll --url=https://placeholder:443 --enrollment-token=token"'
                LogToDir = Get-LogDir
            }
            MSIUninstallParameters = @{
                LogToDir = Get-LogDir
            }
        }
    )
}

# Stuff starting in this section is usable in tests
BeforeAll {
    #$VerbosePreference = "Continue"
    Import-Module (Join-Path $PSScriptRoot "helpers.ps1") -Force -Verbose:$False

    Function Check-AgentRemnants {
        Is-AgentBinaryPresent | Should -BeFalse -Because "The agent should have been cleaned up already"
        Is-AgentFleetEnrolled | Should -BeFalse -Because "The agent should have been cleaned up already"
        Is-AgentServiceRunning | Should -BeFalse -Because "The agent should have been cleaned up already"
        Is-AgentServicePresent | Should -BeFalse -Because "The agent should have been cleaned up already"
        Is-AgentUninstallKeyPresent | Should -BeFalse -Because "The agent should have been cleaned up already"
        Is-AgentInstallCachePresent | Should -BeFalse -Because "The agent should have been cleaned up already"
    }

    # Perform an initial environment cleanup
    try {
        Check-AgentRemnants
    }
    catch {
        write-warning "Found Agent remnants before tests started. Removing Agent."
        Clean-ElasticAgent
    }
}

Describe 'Elastic Agent MSI Installer' {
    # Ideally, all tests perfectly clean up after themselves but if the previous test failed we want to perform our own
    # clean-up but we don't want that clean-up to count against our new test.
    BeforeEach {
        try {
            Check-AgentRemnants
        }
        catch {
            write-warning "Found Agent remnants between tests. Removing Agent."
            Clean-ElasticAgent
        }

        # Reduce filesystem async race conditions - spooky voodoo
        Write-VolumeCache c
        Start-Sleep -Seconds 5
    }

    # All tests in these sections must remove Agent before finishing. If they do not, the test will fail
    AfterEach {
        Check-AgentRemnants

        Has-AgentInstallerProcess | Should -BeFalse -Because "There should be no dangling installer processes after a test run"
    }

    # Using the test cases defined up above we test the agent in Default, Standalone and Fleet modes
    Context "Basic Tests for <Mode> mode" -ForEach $Testcases {
        
        BeforeAll {
            Function Assert-AgentHealthy {
                & $HealthFunction
            }
        }
        <#
        It 'Can be installed in <Mode> mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy
            
            # We clean-up with a clean-up script so that we can differentiate installer from uninstaller failures with the next test
            Clean-ElasticAgent

            Check-AgentRemnants
        }

        It 'Can be installed twice in <Mode> mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy
            
            { Install-MSI -Path $PathToLatestMSI @MSIInstallParameters } | Should -Not -Throw

            # We clean-up with a clean-up script so that we can differentiate installer from uninstaller failures with the next test
            Clean-ElasticAgent

            Check-AgentRemnants
        }
#>

        It 'Can be installed and uninstalled via MSI, with installargs, in <Mode> mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy

            Uninstall-MSI -Path $PathToLatestMSI @MSIUninstallParameters -Flags 'INSTALLARGS="-v"'

            Check-AgentRemnants
        }

        It 'Can be installed and uninstalled via GUID, without installargs, in <Mode> mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy

            { Get-AgentUninstallGUID } | Should -Not -Throw
            $UninstallGuid = Get-AgentUninstallGUID

            Uninstall-MSI -Guid $UninstallGuid @MSIUninstallParameters

            Check-AgentRemnants
        }
        
        It 'Gracefully handles existing agent components' {

            # Create a fake service
            new-service -Name "Elastic Agent" -BinaryPathName "cmd.exe" -DisplayName "Fake Service" -StartupType manual

            # Create a fake directory
            new-item -itemtype directory "C:\Program Files\Elastic\Agent\data\elastic-agent-03ef9d\logs"
            
            # Create a fake config
            set-content "C:\Program Files\Elastic\Agent\elastic-agent.exe" -value "test" -nonewline

            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            # Remove our fakes and see if everything passes 
            if ((get-content "C:\Program Files\Elastic\Agent\elastic-agent.exe" -raw) -eq "test") {
                remove-item "C:\Program Files\Elastic\Agent\elastic-agent.exe"
            }
            if ((Get-Service "Elastic Agent" -Erroraction SilentlyContinue).DisplayName -eq "Fake Service") {
                Remove-Service "Elastic Agent"
            }

            Assert-AgentHealthy

            # We clean-up with a clean-up script so that we can differentiate installer from uninstaller failures with the next test
            Clean-ElasticAgent

            Check-AgentRemnants
        }
        It 'Blocks downgrades and upgrades in <Mode> mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy
            
            { Install-MSI -Path $PathToEarlyMSI -Interactive "/qn" @MSIInstallParameters } | Should -Throw

            Assert-AgentHealthy

            Uninstall-MSI -Path $PathToLatestMSI @MSIUninstallParameters

            Check-AgentRemnants
        }

        It 'Blocks upgrades in <Mode> mode' {
            Install-MSI -Path $PathToEarlyMSI @MSIInstallParameters

            Assert-AgentHealthy

            { Install-MSI -Path $PathToLatestMSI -Interactive "/qn" @MSIInstallParameters } | Should -Throw

            Assert-AgentHealthy

            Uninstall-MSI -Path $PathToEarlyMSI

            Check-AgentRemnants
        }
    }

    Context "Specific tests for Default Mode (Without InstallArgS)" -Foreach $Testcases[0] {
        BeforeAll {
            Function Assert-AgentHealthy {
                & $HealthFunction
            }
        }

    }
    
    Context "Specific tests for Standalone Mode (with installargS=-v)" -Foreach $Testcases[1] {
        BeforeAll {
            Function Assert-AgentHealthy {
                & $HealthFunction
            }
        }
        It 'Behaves itself as a standalone agent' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy

            Uninstall-MSI -Path $PathToLatestMSI @MSIUninstallParameters

            Check-AgentRemnants
        }
        
        It 'Can be installed in Standalone mode and uninstalled via GUID in default mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy

            { Get-AgentUninstallGUID } | Should -Not -Throw
            $UninstallGuid = Get-AgentUninstallGUID

            Uninstall-MSI -Guid $UninstallGuid @MSIUninstallParameters

            Check-AgentRemnants
        }

        It 'Can be installed and uninstalled via MSI, with invalid installargs, in standalone mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy

            { Uninstall-MSI -Path $PathToLatestMSI @MSIUninstallParameters -Flags 'INSTALLARGS="--delayenroll"' } | Should -Throw
            
            Uninstall-MSI -Path $PathToLatestMSI @MSIUninstallParameters -Flags 'INSTALLARGS="-v"'

            Check-AgentRemnants
        }
    }

    Context "Specific tests for Fleet Mode with delayed enroll" -Foreach $Testcases[2] {
        BeforeAll {
            Function Assert-AgentHealthy {
                & $HealthFunction
            }
        }
        
        It 'Rollback uninstall when elastic-agent uninstall crashes' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            # Start the MSI in a background job so that we can kill a child process and measure success
            $Job = Start-Job -WorkingDirectory $PSScriptRoot -ScriptBlock {
                $arglist = "/x $using:PathToLatestMSI /qb"

                write-information "msiexec $arglist"
                $process = start-process -FilePath "msiexec.exe" -ArgumentList $arglist -wait -passthru

                write-output $process.ExitCode
            } 

            $Time = 0
            while (-not (get-process "elastic-agent" -erroraction silentlycontinue) -and $Time -lt 45) {
                start-sleep -seconds 1
                $Time ++
            }

            $Time | Should -BelessThan 45 -Because "otherwise we timed out waiting for the agent"

            Stop-Process -name "elastic-agent"
            
            $Job | Wait-Job

            $Result = $Job | Receive-Job 

            # The interrupted uninstall should fail with a 1603
            $Result | Should -Be 1603

            Check-AgentRemnants
        }

        It 'Rollback install when elastic-agent install crashes' {
            # Start the MSI in a background job so that we can kill a child process and measure success
            $Job = Start-Job -WorkingDirectory $PSScriptRoot -ScriptBlock {
                $arglist = "/i $using:PathToLatestMSI /qb INSTALLARGS=""--delay-enroll --url=https://placeholder:443 --enrollment-token=token"""
                write-information "msiexec $arglist "
                $process = start-process -FilePath "msiexec.exe" -ArgumentList $arglist -wait -passthru

                write-output $process.ExitCode
            } 

            $Time = 0
            while (-not (get-process "elastic-agent" -erroraction silentlycontinue) -and $Time -lt 45) {
                start-sleep -seconds 1
                $Time ++
            }

            $Time | Should -BelessThan 45 -Because "otherwise we timed out waiting for the agent"


            Stop-Process -name "elastic-agent"
            
            $Job | Wait-Job

            $Result = $Job | Receive-Job 

            # The interrupted install should fail with a 1603
            $Result | Should -Be 1603

            # QUIRK: Clean-up the leftovers as elastic-agent install does not fully rollback during failure
            Clean-ElasticAgentDirectory

            Check-AgentRemnants
        }

        It 'Behaves itself as an agent connected to a nonexistent fleet' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy
            Has-AgentFleetEnrollmentAttempt | Should -BeTrue

            Uninstall-MSI -Path $PathToLatestMSI @MSIUninstallParameters

            Check-AgentRemnants
        }


        It 'Can be installed and uninstalled via MSI, with invalid installargs, in fleet mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy

            { Uninstall-MSI -Path $PathToLatestMSI -LogToDir $MSIUninstallParameters.LogToDir -Flags 'INSTALLARGS="--delayenroll"' } | Should -Throw
            
            Uninstall-MSI -Path $PathToLatestMSI @MSIUninstallParameters -Flags 'INSTALLARGS="-v"'

            Check-AgentRemnants
        }

        It 'Can be installed in Fleet mode and uninstalled via GUID in default mode' {
            Install-MSI -Path $PathToLatestMSI @MSIInstallParameters

            Assert-AgentHealthy

            { Get-AgentUninstallGUID } | Should -Not -Throw
            $UninstallGuid = Get-AgentUninstallGUID

            Uninstall-MSI -Guid $UninstallGuid @MSIUninstallParameters -Flags '/norestart'

            Check-AgentRemnants
        }
    }
}