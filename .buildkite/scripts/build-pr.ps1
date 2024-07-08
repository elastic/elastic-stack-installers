$ErrorActionPreference = "Stop"
Set-Strictmode -version 3

$eligibleReleaseBranchesMajorMinor = "^[89]+\.[0-9]+"
$runTests = $true

function setManifestUrl {
    param (
        [string]$targetBranch
    )

    # the API url (snapshot or staging) expects master where we normally use main
    $ApiTargetBranch = if ($targetBranch -eq "main") { "master" } else { $targetBranch }

    $artifactsUrl = "https://snapshots.elastic.co/latest/${ApiTargetBranch}.json"

    Write-Host "Artifacts url is: $artifactsUrl"
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $artifactsUrl
        $responseJson = $response.Content | ConvertFrom-Json
        $env:MANIFEST_URL = $responseJson.manifest_url
        Write-Host "Using MANIFEST_URL=$env:MANIFEST_URL"
    } catch {
        $errorMessage = "There was an error parsing manifest_url from $artifactsUrl. Exiting."
        throw $errorMessage
    }
}

$targetBranch = $env:BUILDKITE_PULL_REQUEST_BASE_BRANCH
if ( ($targetBranch -ne "main") -and -not ($targetBranch -match $eligibleReleaseBranchesMajorMinor)) {
    Write-Host "^^^ +++"
    $errorMessage = "This PR is targetting the [$targetBranch] branch, but running tests is only supported against `main` or $eligibleReleaseBranchesMajorMinor branches. Exiting."
    Write-Host $errorMessage
    throw $errorMessage
}

setManifestUrl -targetBranch $targetBranch

# workaround path limitation for max 248 characters
# example: https://buildkite.com/elastic/elastic-stack-installers/builds/3104#018c5e1b-23a7-4330-ad5d-4acc69157822/74-180
cd ..
# we can't use Rename-Item because this script runs from within the existing checkout resulting in
# Rename-Item : The process cannot access the file because it is being used by another process.
Copy-Item -Path .\elastic-stack-installers -Destination c:\users\buildkite\esi -Recurse
cd c:\users\buildkite\esi

# Read the stack version from build properties
[xml]$xml = Get-Content -Path "Directory.Build.props"
$ns = New-Object Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("ns", "http://schemas.microsoft.com/developer/msbuild/2003")
$stack_version = $xml.SelectSingleNode("//ns:PropertyGroup/ns:StackVersion", $ns).InnerText
$version = $stack_version + "-SNAPSHOT"

Write-Host "~~~ Building Stack version: $stack_version"

Write-Output "~~~ Installing dotnet-sdk"
& "./tools/dotnet-install.ps1" -NoPath -JSonFile global.json -Architecture "x64" -InstallDir c:/dotnet-sdk
${env:PATH} = "c:\dotnet-sdk;" + ${env:PATH}
Get-Command dotnet | Select-Object -ExpandProperty Definition

$client = new-object System.Net.WebClient
$currentDir = $(Get-Location).Path
$beats = @('auditbeat', 'filebeat', 'heartbeat', 'metricbeat', 'packetbeat', 'winlogbeat')
$ossBeats = $beats | ForEach-Object { $_ + "-oss" }

# TODO remove (and all references/conditionals below) after testing; this just helps to speed up elastic-agent specific build tests
$onlyAgent = $env:ONLY_AGENT -as [string]

Remove-Item bin/in -Recurse -Force -ErrorAction Ignore
New-Item bin/in -Type Directory -Force

$manifestUrl = ${env:MANIFEST_URL}
$response = Invoke-WebRequest -UseBasicParsing -Uri $manifestUrl
$json = $response.Content | ConvertFrom-Json

Write-Output "~~~ Downloading $workflow dependencies"

$urls = @()

try {
    $packageName = "elastic-agent"
    $projectName = "$packageName-package"
    $urls += $json.projects.$projectName.packages."$packageName-$version-windows-x86_64.zip".url

    if ($onlyAgent -eq "true") {
        Write-Output "Skipping beats because env var ONLY_AGENT is set to [$env:ONLY_AGENT]"
    }
    else {
        $projectName = "beats"
        foreach ($packageName in ($beats + $ossBeats)) {
            $urls += $json.projects.$projectName.packages."$packageName-$version-windows-x86_64.zip".url
        }
    }
    foreach ($url in $urls) {
        $destFile = [System.IO.Path]::GetFileName($url)
        Write-Output "Downloading from $url to $currentDir/bin/in/$destFile"
        $client.DownloadFile(
            $url,
            "$currentDir\bin\in\$destFile"
        )
    }
} catch [System.Net.WebException] {
    if ($_.Exception.InnerException) {
        Write-Error $_.Exception.InnerException.Message
    } else {
        Write-Error $_.Exception.Message
    }
    throw "An error was encountered while downloading dependencies, aborting."
}

Remove-Item bin/out -Recurse -Force -ErrorAction Ignore

Write-Output "--- Building $workflow msi"
$cliArgs = @(
    "run",
    "--project",
    "src\build\ElastiBuild.csproj",
    "-c",
    "Release",
    "--",
    "build",
    "--cid",
    $version
)
$cliArgs += "elastic-agent"
if ($onlyAgent -ne "true") {
    $cliArgs += ($beats + $ossBeats)
}

&dotnet $cliArgs
if ($LastExitcode -ne 0) {
    Write-Error "Build $workflow failed with exit code $LastExitcode"
    exit $LastExitcode
} else {
    Write-Output "Build $workflow completed with exit code $LastExitcode"
}

Write-Output "--- Checking that all artifacts are there"
$msiCount = Get-ChildItem bin/out -Include "*.msi" -Recurse | Measure-Object | Select-Object -ExpandProperty Count

$expected = 1
if ($onlyAgent -ne "true") {
    $expected += (2 * $beats.Length)
}

if ($msiCount -ne $expected) {
    Write-Error "Failed: expected $expected msi executables to be produced, but $msiCount were found."
    exit 1
} else {
    Write-Output "Success, found $msiCount artifacts in bin/out."
}
