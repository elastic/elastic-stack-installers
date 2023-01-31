$stack_version="8.7.0"

& "./tools/dotnet-install.ps1" -NoPath -JSonFile global.json -Architecture "x64" -InstallDir c:/dotnet-sdk
echo "~~~ Installing dotnet-sdk"
& "./tools/dotnet-install.ps1" -NoPath -Version 5.0.408 -Architecture "x64" -InstallDir c:/dotnet-sdk
${env:PATH} = "c:\dotnet-sdk;" + ${env:PATH}
Get-Command dotnet | Select-Object -ExpandProperty Definition

echo "~~~ Reading msi certificate from vault"
$MsiCertificate=& vault read -field=cert secret/ci/elastic-elastic-stack-installers/msi
$MsiPassword=& vault read -field=password secret/ci/elastic-elastic-stack-installers/msi
Remove-Item Env:VAULT_TOKEN

$cert_home="C:/.cert"
New-Item $cert_home -Type Directory -Force
[IO.File]::WriteAllBytes("$cert_home/msi_certificate.p12", [Convert]::FromBase64String($MsiCertificate))
[IO.File]::WriteAllText("$cert_home/msi_password.txt", $MsiPassword)
echo "Certificate successfully written to $cert_home"

$client = new-object System.Net.WebClient
$currentDir = $(Get-Location).Path
$beats = @('auditbeat', 'filebeat', 'heartbeat', 'metricbeat', 'packetbeat', 'winlogbeat')
$ossBeats = $beats | ForEach-Object { $_ + "-oss" }
foreach ($kind in @("-SNAPSHOT")) {
    echo "~~~ downloading beat dependencies$kind"
    Remove-Item bin/in -Recurse -Force -ErrorAction Ignore
    New-Item bin/in -Type Directory -Force
    $version = $stack_version + $kind
    $response = Invoke-WebRequest -UseBasicParsing -Uri "https://artifacts-api.elastic.co/v1/versions/$version/builds/latest"
    $json = $response.Content | ConvertFrom-Json
    $buildId = $json.build.build_id
    if ($kind -eq "") {
        $hostname = "staging.elastic.co"
    } else {
        $hostname = "snapshots.elastic.co"
    }
    foreach ($beat in ($beats + $ossBeats)) {
        try {
            $beatName = $beat.Replace("-oss", "")
            $url = "https://$hostname/$buildId/downloads/beats/$beatName/$beat-$version-windows-x86_64.zip"
            echo "Downloading from $url"
            $client.DownloadFile(
               $url,
               "$currentDir/bin/in/$beat-$version-windows-x86_64.zip"
            )
        }
        catch [System.Net.WebException] {
            if ($_.Exception.InnerException) {
               Write-Error $_.Exception.InnerException.Message
            } else {
               Write-Error $_.Exception.Message
            }
            throw "An error was encountered while downloading dependencies, aborting."
        }
    }


    echo "--- Building msi$kind"
    New-Item bin/out -Type Directory -Force
    $args = @(
        "run",
        "--project",
        "src\build\ElastiBuild.csproj",
        "-c",
        "Release",
        "--",
        "build",
        "--cid",
        $version,
        "--cert-file",
        "$cert_home/msi_certificate.p12",
        "--cert-pass",
        "$cert_home/msi_password.txt"
    )
    $args += ($beats + $ossBeats)
    echo "Starting processs"
    &dotnet $args
    if ($LastExitcode -ne 0) {
        Write-Error "Build$kind failed with exit code $LastExitcode"
        exit $exitCode
    } else {
        echo "Build$kind completed with exit code $LastExitcode"
    }
    $logFile = "bin\out\logfile.csv"

    Get-WinEvent -FilterHashtable @{ LogName = 'Microsoft-Windows-Sysmon/Operational'; Id = 13 } |
        Export-Csv $logFile 

    $msiCount = Get-ChildItem bin/out -Include "*.msi" -Recurse | Measure-Object | Select-Object -ExpandProperty Count
    if ($msiCount -ne 8) {
        Write-Error "Expected 8 msi executable to be produced, but $msiCount were"
        exit 1
    }
}