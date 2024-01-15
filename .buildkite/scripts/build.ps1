# workaround path limitation for max 248 characters
# example: https://buildkite.com/elastic/elastic-stack-installers/builds/3104#018c5e1b-23a7-4330-ad5d-4acc69157822/74-180
cd ..
# we can't use Rename-Item because this script runs from within the existing checkout resulting in
# Rename-Item : The process cannot access the file because it is being used by another process.
Copy-Item -Path .\elastic-stack-installers -Destination esi -Recurse
cd esi

# Read the stack version from build properties
[xml]$xml = Get-Content -Path "Directory.Build.props"
$ns = New-Object Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("ns", "http://schemas.microsoft.com/developer/msbuild/2003")
$stack_version = $xml.SelectSingleNode("//ns:PropertyGroup/ns:StackVersion", $ns).InnerText
Write-Host "Building Stack version: $stack_version"

echo "~~~ Installing dotnet-sdk"
& "./tools/dotnet-install.ps1" -NoPath -JSonFile global.json -Architecture "x64" -InstallDir c:/dotnet-sdk
${env:PATH} = "c:\dotnet-sdk;" + ${env:PATH}
Get-Command dotnet | Select-Object -ExpandProperty Definition

echo "~~~ Reading msi certificate from vault"
$MsiCertificate=& vault read -field=cert secret/ci/elastic-elastic-stack-installers/signing_cert
$MsiPassword=& vault read -field=password secret/ci/elastic-elastic-stack-installers/signing_cert
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
$agent = ${env:AGENT}

Remove-Item bin/in -Recurse -Force -ErrorAction Ignore
New-Item bin/in -Type Directory -Force

echo "~~~ downloading $workflow dependencies"

$uri = ""

if ($agent -eq "true") {
    echo "Agent is true, running Elastic agent MSI builder"
    $version = $stack_version + "-" + $workflow.ToUpper()
    $hostname = if ($workflow -eq "snapshot") {"artifacts-snapshot.elastic.co"} Else {"artifacts-staging.elastic.co"}
    $uri = "https://$hostname/elastic-agent-package/latest/$version.json"
} elseif ($workflow -eq "snapshot") {
    $version = $stack_version + "-" + $workflow.ToUpper()
    $hostname = "artifacts-snapshot.elastic.co"
    $uri = "https://$hostname/beats/latest/$version.json"
} else {
    $version = $stack_version
    $hostname = "artifacts-staging.elastic.co"
    $uri = "https://$hostname/beats/latest/$version.json"
}

echo "uri: $uri"
$response = Invoke-WebRequest -UseBasicParsing -Uri $uri
$json = $response.Content | ConvertFrom-Json
$buildId = $json.build_id
$prefix = if ($agent -eq "true") {"$hostname/elastic-agent-package/$buildId"} Else {"$hostname/beats/$buildId"}
echo "prefix $prefix"

try {
    if ($agent -eq "true") {
        $beatName = "elastic-agent"
        $url = "https://$prefix/downloads/beats/$beatName/$beatName-$version-windows-x86_64.zip"
        echo "Downloading from $url"
        $client.DownloadFile(
            $url,
            "$currentDir/bin/in/$beatName-$version-windows-x86_64.zip"
        )
    } else {
        foreach ($beat in ($beats + $ossBeats)) {
            $beatName = $beat.Replace("-oss", "")
            $url = "https://$prefix/downloads/beats/$beatName/$beat-$version-windows-x86_64.zip"
            echo "Downloading from $url"
            $client.DownloadFile(
                $url,
                "$currentDir/bin/in/$beat-$version-windows-x86_64.zip"
            )
        }
    }
} catch [System.Net.WebException] {
    if ($_.Exception.InnerException) {
        Write-Error $_.Exception.InnerException.Message
    } else {
        Write-Error $_.Exception.Message
    }
    throw "An error was encountered while downloading dependencies, aborting."
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
if ($agent -eq "true") {
    $args += "elastic-agent"
} else {
    $args += ($beats + $ossBeats)
}

&dotnet $args
if ($LastExitcode -ne 0) {
    Write-Error "Build$workflow failed with exit code $LastExitcode"
    exit $LastExitcode
} else {
    echo "Build$workflow completed with exit code $LastExitcode"
}

echo "--- Checking that all artifacts are there"
$msiCount = Get-ChildItem bin/out -Include "*.msi" -Recurse | Measure-Object | Select-Object -ExpandProperty Count
$expected = if ($agent -eq "true") {1} Else {2 * $beats.Length}
if ($msiCount -ne $expected) {
    Write-Error "Expected $expected msi executable to be produced, but $msiCount were"
    exit 1
} else {
    echo "found $msiCount artifacts in bin/out"
}
