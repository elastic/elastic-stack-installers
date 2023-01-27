$stack_version="8.7.0"

echo "~~~ Installing dotnet-sdk"
& "./tools/dotnet-install.ps1" -NoPath -JSonFile global.json -Architecture "x64" -InstallDir c:/dotnet-sdk
${env:PATH} = "c:\dotnet-sdk;" + ${env:PATH}


echo "--- build show"
./build show

echo "~~~ Reading msi certificate from vault"
$MsiCertificate=& vault read -field=cert secret/ci/elastic-elastic-stack-installers/msi
$MsiPassword=& vault read -field=password secret/ci/elastic-elastic-stack-installers/msi
Remove-Item Env:VAULT_TOKEN

$cert_home="C:/.cert"
New-Item $cert_home -Type Directory -Force
[IO.File]::WriteAllBytes("$cert_home/msi_certificate.p12", [Convert]::FromBase64String($MsiCertificate))
[IO.File]::WriteAllText("$cert_home/msi_password.txt", $MsiPassword)


$client = new-object System.Net.WebClient
$currentDir = $(Get-Location).Path
$beats = @('auditbeat', 'filebeat', 'heartbeat', 'metricbeat', 'packetbeat', 'winlogbeat')
$ossBeats = $beats | ForEach-Object { $_ + "-oss" }
foreach ($kind in @("-SNAPSHOT", "")) {
    echo "~~~ downloading beat dependencies$kind"
    Remove-Item bin/in -Recurse -Force -ErrorAction Ignore
    New-Item bin/in -Type Directory -Force
    $version = $stack_version + $kind
    if ($kind == "") {
        $hostname = "snapshot.elastic.co"
    } else {
        $hostname = "staging.elastic.co"
    }
    foreach ($beat in ($beats + $ossBeats)) {
        try {
            $client.DownloadFile(
               "https://$hostname/$version/downloads/beats/$beat/$beat-$version-windows-x86_64.zip",
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
    args=@(
      "build",
      "--cid", version,
      "--cert-file", "$cert_home/msi_certificate.p12",
      "--cert-pass", "$cert_home/msi_password.txt"
    )
    Start-Process -FilePath ./build -ArgumentList $beats -Wait
}