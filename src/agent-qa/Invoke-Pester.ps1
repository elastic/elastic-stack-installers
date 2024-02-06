#Requires -RunAsAdministrator
param (
    $PathToLatestMSI = (Join-Path $PSScriptRoot "bin/elastic-agent-8.13.0-windows-x86_64.msi"),
    $PathToEarlyMSI = (Join-Path $PSScriptRoot "bin/elastic-agent-8.11.4-windows-x86_64.msi")
)


Set-Strictmode -version 3
$ErrorActionPreference = "Stop"

Start-Transcript -Path pester.log -Append -IncludeInvocationHeader

# Passing data to pester requires version 5.1.0
try {
    Import-Module Pester -MinimumVersion 5.1.0 
} catch {
    Write-warning "Pester 5.1.0 or later is required. Installing now."
    Install-Module -Name Pester -Force -Scope CurrentUser -SkipPublisherCheck
}
# Passing data to pester requires version 5.1.0
try {
    Import-Module FindOpenFile
} catch {
    Write-warning "FindOpenFile is required. Installing now."
    Install-Module -Name FindOpenFile -Force -Scope CurrentUser -SkipPublisherCheck
}

# Clean-up our environment
$testsdir = $PSScriptRoot
$logsdir = Join-Path $PSScriptRoot "logs"

remove-item $logsdir -Recurse -ErrorAction SilentlyContinue
new-item $logsdir -ItemType directory -ErrorAction SilentlyContinue

# Data to pass to tests
$data =  @{
    PathToEarlyMSI = $PathToEarlyMSI
    PathToLatestMSI = $PathToLatestMSI
    LogsDir = $logsdir
    VerbosePreference = "continue" # Comment out to disable verbose logging during test runs
} 

if (-not (test-path ($Data.PathToEarlyMSI))) {
    throw "Missing early MSI version for upgrade testing"
}
if (-not (test-path ($Data.PathToLatestMSI))) {
    throw "Missing latest MSI version for upgrade testing"
}

$container = New-PesterContainer -Path $testsdir\*.tests.ps1 -Data $data

$config = [PesterConfiguration] @{
    Run = @{
        Throw = $True
        Container = $container
    }
    
    Output = @{
        Verbosity = "Detailed"
    }
}

Invoke-Pester -Configuration $config

Stop-Transcript