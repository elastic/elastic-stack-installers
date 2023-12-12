$AgentMSI = Get-ChildItem bin/out -Include "elastic-agent*.msi" -Recurse | Measure-Object | Select-Object -ExpandProperty Count

if ($AgentMSI -eq $null) {
    write-error "No agent MSI found to test"
}

& (Join-Path $PSScriptRoot "../../src/agent-qa/Invoke-Pester.ps1") -PathToLatestMSI $AgentMSI.Fullname