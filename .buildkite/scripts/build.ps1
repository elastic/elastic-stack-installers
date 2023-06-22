$stack_version="7.17.11"

echo "~~~ Installing dotnet-sdk"
& "./tools/dotnet-install.ps1" -NoPath -JSonFile global.json -Architecture "x64" -InstallDir c:/dotnet-sdk
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
$workflow = ${env:WORKFLOW}

echo "~~~ downloading beat $workflow dependencies"
Remove-Item bin/in -Recurse -Force -ErrorAction Ignore
New-Item bin/in -Type Directory -Force
if ($workflow -eq "snapshot") {
    $version = $stack_version + "-" + $workflow.ToUpper()
    $response = Invoke-WebRequest -UseBasicParsing -Uri "https://artifacts-api.elastic.co/v1/versions/$version/builds/latest"
    $json = $response.Content | ConvertFrom-Json
    $buildId = $json.build.build_id
    $hostname = "snapshots.elastic.co"
    $prefix = "$hostname/$buildId"
} else {
    $version = $stack_version
    $hostname = "staging.elastic.co"
    $buildId = "7.17.11-23f9a819"
    $prefix = "$hostname/$buildId"
}
foreach ($beat in ($beats + $ossBeats)) {
    try {
        $beatName = $beat.Replace("-oss", "")
        $url = "https://$prefix/downloads/beats/$beatName/$beat-$version-windows-x86_64.zip"
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

echo "--- Building $workflow msi"
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
&dotnet $args
if ($LastExitcode -ne 0) {
    Write-Error "Build$workflow failed with exit code $LastExitcode"
    exit $LastExitcode
} else {
    echo "Build$workflow completed with exit code $LastExitcode"
}

echo "--- Checking that all artefacts are there"
$msiCount = Get-ChildItem bin/out -Include "*.msi" -Recurse | Measure-Object | Select-Object -ExpandProperty Count
$expected = 2 * $beats.Length
if ($msiCount -ne $expected) {
    Write-Error "Expected $expected msi executable to be produced, but $msiCount were"
    exit 1
}
