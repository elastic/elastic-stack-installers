$ErrorActionPreference = "Stop"
Set-Strictmode -version 3

write-host (ConvertTo-Json $PSVersiontable -Compress)
write-host "Running as: $([Environment]::UserName)"

write-host "`$env:AGENT = $($Env:AGENT)"

if ($env:AGENT -ne "true") {
    write-host "Not expecting to test an Agent build in this configuration."
    return
}

if ($psversiontable.psversion -lt "7.4.0") {
    # Download Powershell Core, and rerun this script using Powershell Core
    
    write-host "Downloading Powershell Core"
    invoke-webrequest -uri https://github.com/PowerShell/PowerShell/releases/download/v7.4.0/PowerShell-7.4.0-win-x64.zip -outfile pwsh.zip
    
    write-host "Expanding Powershell Core"
    Expand-Archive pwsh.zip -destinationpath (Join-Path $PSScriptRoot "pwsh")
    
    Write-host "Invoking from Powershell Core"
    & (Join-Path $PSScriptRoot "pwsh/pwsh.exe") -file (Join-Path $PSScriptRoot "test.ps1")
    
    return
}

$AgentMSI = Get-ChildItem bin/out -Include "elastic-agent*.msi" -Recurse

if ($AgentMSI -eq $null) {
    write-error "No agent MSI found to test"
}


$OldAgentMSI = (Join-Path $PSScriptRoot "elastic-agent-8.10.4-windows-x86_64.msi")
if (-not (test-path $OldAgentMSI)) {
    Write-Host "Downloading older MSI for upgrade tests"
    invoke-webrequest -uri https://storage.googleapis.com/agent-msi-testing/elastic-agent-8.10.4-windows-x86_64.msi -outfile $OldAgentMSI
}

& (Join-Path $PSScriptRoot "../../src/agent-qa/Invoke-Pester.ps1") -PathToLatestMSI $AgentMSI.Fullname -PathToEarlyMSI $OldAgentMSI

write-host "Returned from Pester Test"