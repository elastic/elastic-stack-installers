$Script:AgentInstallCache = "C:\Program Files\Elastic\Beats"
$Script:AgentPath = "C:\Program Files\Elastic\Agent"
$Script:AgentBinary = "elastic-agent.exe"

$Script:LogDir = (Join-Path $PSScriptRoot "logs")

Function Get-LogDir {
    return $Script:LogDir
}

Function Get-AgentUninstallRegistryKey {
    param (
        [switch] $Passthru
    )

    $Result = @(@(Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\") | Where-Object {
            $_.GetValue("DisplayName") -Like "Elastic Agent *"
        })
    if ($Result.Length -eq 0) { return; }

    if ($Passthru) { return $Result }
    
    return $Result.Name
}

Function Get-AgentUninstallGUID {
    $RegistryKey = Get-AgentUninstallRegistryKey -Passthru

    try {
        $GUID = $RegistryKey.PSChildName
        return $GUID
    }
    catch {}
}

Function Run-AgentChecks {
    param (
        $Functions,
        [switch] $ReturnErrors,
        [switch] $ShouldFail
    )

    $Errors = @()
    foreach ($AgentFunction in $Functions) {
        $Success = & $AgentFunction
        if ($ShouldFail) {
            if ($Success) {
                $Errors += "$($AgentFunction.Ast.Name)"
            }
        }
        else {
            if (-Not $Success) {
                $Errors += "$($AgentFunction.Ast.Name)"
            }
        }
    }

    if ($ReturnErrors) { return $Errors }

    return ($Errors.length -eq 0)
}

Function Is-AgentServicePresent {
    
    if (Get-Service -Name "Elastic Agent" -ErrorAction SilentlyContinue) {
        return $true
    }

    return $false
}

Function Is-AgentServiceRunning {
    param (
        [switch] $Resolve,
        [switch] $WaitForExit
    )
    
    if (-not (Is-AgentServicePresent)) { return $False }

    $agent = Get-Service -Name "Elastic Agent" -ErrorAction SilentlyContinue
    
    if ($agent.Status -eq "Running") {
        return $true
    } elseif ($resolve) {
        Start-Service "Elastic Agent"
        if ($WaitForExit) {
            Wait-Process -Name "elastic-agent" -Timeout 30 -ErrorAction SilentlyContinue
        } else {
            Start-Sleep -Seconds 3
        }
        return (Is-AgentServiceRunning)
    }

    return $false
}

Function Is-AgentAttemptingEnrollment {
    if (-not (Is-AgentProducingLogs)) { return $null }

    $LogFiles = Get-AgentLogFile -Latest $True

    foreach ($LogFile in $LogFiles) {
        $content = Get-Content $LogFile.FullName

        try {
            $json = convertfrom-json $content -AsHashtable
            if ($json["log.level"] -eq "error") {
                $Errors += $content
            }
        }
        catch {

        }
    }
}

Function Is-AgentLoggingErrors {
    if (-not (Is-AgentProducingLogs)) { return $null }

    $LogFiles = Get-AgentLogFile -Latest $True

    $Errors = @()

    foreach ($LogFile in $LogFiles) {
        $content = Get-Content $LogFile.FullName

        try {
            $json = convertfrom-json $content -AsHashtable
            if ($json["log.level"] -eq "error") {
                $Errors += $content
            }
        }
        catch {

        }
        
        if ($content -match "FATAL") {
            $Errors += $content
        }
    }

    if ($Errors.Count -gt 0) {
        return $false
    }

    return $true
}

# Find Elastic-Agent and msiexec processes running as current user to indicate an ongoing installer action
Function Has-AgentInstallerProcess {
    $msiexec = @(get-process "msiexec" -IncludeUserName -ErrorAction SilentlyContinue | Where-Object {$_.Username -ne "NT AUTHORITY\SYSTEM" })

    if ($msiexec.length -ne 0) { return $true}

    $msiexec = @(get-process "elastic-agent" -IncludeUserName -ErrorAction SilentlyContinue | Where-Object {$_.Username -ne "NT AUTHORITY\SYSTEM" })

    if ($msiexec.length -ne 0) { return $true}

    return $False
}

Function Has-AgentLogged {

    foreach ($i in 0..5) {

        try {
            $LogFile = Get-AgentLogFile -Latest $True
            return $true
        } catch {}
        
        write-host "Waiting for agent to log message"
        start-sleep -seconds 3
    }
    
    return $false
}

Function Is-AgentLogGrowing {
    try {
        $LogFile = Get-AgentLogFile -Latest $True
    }
    catch {
        return $false
    }

    $Timeout = 15
    $Time = 0
    $PreviousSize = (Get-Item $LogFile).Length

    while ($Time -lt $Timeout) {
        $CurrentSize = (Get-Item $LogFile).Length
    
        if ($CurrentSize -gt $PreviousSize) {
            return $true
        }

        $Time ++
        start-sleep -seconds 1
    }

    return $false
}

Function Has-AgentStandaloneLog {
    try {
        $LogFile = Get-AgentLogFile -Latest $True
    }
    catch {
        return $false
    }

    $Content = Get-Content -raw $LogFile

    foreach ($i in 0..5) {
        if ($Content -like "*Parsed configuration and determined agent is managed locally*") {
            return $True
        }

        write-host "Waiting for agent to log standalone message"
        start-sleep -seconds 3
    }

    return $false
}

Function Has-AgentFleetEnrollmentAttempt {
    try {
        $LogFile = Get-AgentLogFile -Latest $True
    }
    catch {
        return $false
    }

    $Content = Get-Content -raw $LogFile

    if ($Content -like "*failed to perform delayed enrollment: fail to enroll: fail to execute request to fleet-server: lookup placeholder: no such host*") {
        return $True
    }

    return $false
}

Function Is-AgentFleetEnrolled {
    $path = (Join-Path $Script:AgentPath "fleet.enc")
    if (Test-Path $path) {
        return $true
    }
    return $false
}

Function Is-AgentUninstallKeyPresent {

    $RegistryKey = @(Get-AgentUninstallRegistryKey)
    if ($RegistryKey.length -ne 0) {
        return $true
    }

    Return $false
}

Function Is-AgentInstallCacheGone {
    return (-not (Is-AgentInstallCachePresent))
}

Function Is-AgentInstallCachePresent {
    $path = $Script:AgentInstallCache
    if (Test-Path $path) {
        return $true
    }
    return $false
}

Function Get-AgentInstallCacheCount {
    $path = $Script:AgentInstallCache
    if (Test-Path $path) {
        return @(Get-ChildItem -Path $Script:AgentInstallCache -Recurse).Length
    }
    return 0
}

Function Is-AgentBinaryPresent {
    $path = (Join-Path $Script:AgentPath $Script:AgentBinary)
    if (Test-Path $path) {
        return $true
    }
    return $false
}

Function Get-AgentLogFile {
    param (
        $Latest
    )

    
    $LogFiles = @(Get-ChildItem -Path (get-item "C:\Program Files\Elastic\Agent\data\*\logs").fullname -Filter "*.ndjson" | Where-Object {$_.name -notlike "*watcher*"} | Sort-Object LastWriteTime)

    if ($LogFiles.Count -eq 0) {
        throw "No log files found"
    }

    if ($Latest) {
        return $LogFiles | Select-Object -last 1
    } 

    return $LogFiles
}


Function Clean-ElasticAgent {

    try { Clean-ElasticAgentInstaller } catch { }

    Clean-ElasticAgentUninstallKeys

    Clean-ElasticAgentService

    Clean-ElasticAgentProcess

    Clean-ElasticAgentDirectory
}

Function Clean-ElasticAgentInstaller {
    
    $UninstallGuid = Get-AgentUninstallGUID
    if (-not $UninstallGuid) { return }

    Uninstall-MSI -Guid $UninstallGuid -LogToDir (Get-LogDir)
}

Function Clean-ElasticAgentService {

    if (Get-Service 'Elastic Agent' -ErrorAction SilentlyContinue) {
        Stop-Service 'Elastic Agent'
        Remove-Service 'Elastic Agent'
    }
}

Function Clean-ElasticAgentProcess {
    if (Get-Process -Name 'elastic-agent.exe' -erroraction SilentlyContinue) {
        Stop-Process -name "elastic-agent.exe"
    }
}

Function Clean-ElasticAgentDirectory {
    $Path = Join-Path ($Env:ProgramFiles) "Elastic\Agent"
    if (Test-Path $Path) {
        Remove-Item -Recurse -Path $Path -Force
    }

    $Path = Join-Path ($Env:ProgramFiles) "Elastic\Beats"
    if (Test-Path $Path) {
        Remove-Item -Recurse -Path $Path -Force
    }
}

Function Clean-ElasticAgentUninstallKeys {
    $Keys = Get-AgentUninstallRegistryKey -Passthru
    if ($Keys) {
        Remove-Item -Path $Keys
    }
}

Function New-MSIVerboseLoggingDestination {
    param (
        $Destination,
        $Prefix,
        $Suffix
    )

    $Timestamp = get-date -format "yyyyMMdd-hhmmss"
    $Name = (@($Prefix, $Timestamp, $Suffix).Where{ $_ } -join "-") + ".log"
    $SanitizedDestination = Resolve-Path -path $Destination
    $Path = (Join-Path $SanitizedDestination $Name)

    return $Path
}

Function Invoke-MSIExec {
    param (
        $Action,
        $Arguments,
        $LogToDir
    )

    $arglist = "/$Action $($Arguments -join " ")"

    if ($LogToDir) {
        $LoggingDestination = (New-MSIVerboseLoggingDestination -Destination $LogToDir -Suffix $Action)
        $arglist += " /l*v " + $LoggingDestination
    }

    write-verbose "Invoking msiexec.exe $arglist"
    $process = start-process -FilePath "msiexec.exe" -ArgumentList $arglist -PassThru
    $thisPid = $Process.id

    # Wait for the process to exit but only for 10 minutes and then terminate it
    Wait-Process -id $thisPid -Timeout 600 -ErrorVariable timeouted
    if ($timeouted)
    {
        # terminate the process
        write-warning "Killing process $thisPid as it reached timeout ($timeouted)"
        stop-process -id $thisPid
    }

    if ($process.ExitCode -ne 0) {
        $Message = "msiexec reports error $($process.ExitCode) = $(Get-MSIErrorMessage -Code $Process.ExitCode)"
        try {
            if ($LogToDir -and $Action -eq "x") {
                $CustomActionLog = Select-String -Path $LoggingDestination -Pattern 'Calling custom action BeatPackageCompiler!Elastic.PackageCompiler.Beats.AgentCustomAction.UnInstallAction' -Context 0,30
                write-warning "Elastic Agent uninstall returned:"
                write-warning ($CustomActionLog.Context.PostContext -join "`n")
            } elseif ($LogToDir -and $Action -eq "i") {
                $CustomActionLog = Select-String -Path $LoggingDestination -Pattern 'Calling custom action BeatPackageCompiler!Elastic.PackageCompiler.Beats.AgentCustomAction.InstallAction' -Context 0,30
                write-warning "Elastic Agent uninstall returned:"
                write-warning ($CustomActionLog.Context.PostContext -join "`n")
            }
        } catch {
            write-warning "Failed to parse msi log for errors"
            write-warning "Dumping full MSI Log"
            write-host (Get-Content $LoggingDestination -raw)
        }

        write-verbose $Message

        Throw $Message
    }

    write-verbose "msiexec reports success with return code 0."
}

Function Install-MSI {
    param (
        $path,
        $flags,
        $Interactive = "/qn",
        $LogToDir
    )

    $msiArgs = @(@($Path) + $Interactive + $Flags)

    Invoke-MSIExec -Action i -Arguments $msiArgs -LogToDir $LogToDir
}

# Uninstall MSI based on path to MSI or GUID
Function Uninstall-MSI {
    param (
        $path,
        $guid,
        $flags,
        $Interactive = "/qn",
        $LogToDir
    )

    if (-not $Path -and -not $Guid){
        throw "Uninstall-msi called without path to an MSI or a GUID"
    }

    $msiArgs = @($Path,$Guid).Where{$_} + $Interactive + $Flags
    
    $OpenFiles = @(Find-OpenFile | Where-Object {$_.Name -like "*Elastic\Agent*" -or $_.Name -like "*Elastic\Beats*"})
    foreach ($OpenFile in $OpenFiles) {
        write-host "Open file may block uninstall $($OpenFile.Name) with PID $($OpenFile.ProcessID) opened by $((Get-Process -ID $OpenFile.ProcessID).ProcessName)"
    }

    try {
        Invoke-MSIExec -Action x -Arguments $msiArgs -LogToDir $LogToDir
    }
    catch {
        # Find open files in the Elastic\Agent directory
        $OpenFiles = @(Find-OpenFile | Where-Object {$_.Name -like "*Elastic\Agent*" -or $_.Name -like "*Elastic\Beats*"})
        foreach ($OpenFile in $OpenFiles) {
            write-warning "Found open file $($OpenFile.Name) with PID $($OpenFile.ProcessID) opened by $((Get-Process -ID $OpenFile.ProcessID).ProcessName)"
        }
        throw $_
    }
    
}



Function Get-MSIErrorMessage {
    param (
        $Code
    )

    return $Script:CodeToMessage[$Code]
}

$Script:CodeToMessage = @{
    0    = "The action completed successfully."
    13   = "data is invalid."
    87   = "of the parameters was invalid."
    120  = "value is returned when a custom action attempts to call a function that can't be called from custom actions. The function returns the value ERROR_CALL_NOT_IMPLEMENTED."
    1259 = "Windows Installer determines a product might be incompatible with the current operating system, it displays a dialog box informing the user and asking whether to try to install anyway. This error code is returned if the user chooses not to try the installation."
    1601 = "Windows Installer service couldn't be accessed. Contact your support personnel to verify that the Windows Installer service is properly registered."
    1602 = "user canceled installation."
    1603 = "fatal error occurred during installation."
    1604 = "suspended, incomplete."
    1605 = "action is only valid for products that are currently installed."
    1606 = "feature identifier isn't registered."
    1607 = "component identifier isn't registered."
    1608 = "is an unknown property."
    1609 = "handle is in an invalid state."
    1610 = "configuration data for this product is corrupt. Contact your support personnel."
    1611 = "component qualifier not present."
    1612 = "installation source for this product isn't available. Verify that the source exists and that you can access it."
    1613 = "installation package can't be installed by the Windows Installer service. You must install a Windows service pack that contains a newer version of the Windows Installer service."
    1614 = "product is uninstalled."
    1615 = "SQL query syntax is invalid or unsupported."
    1616 = "record field does not exist."
    1618 = "installation is already in progress. Complete that installation before proceeding with this install. For information about the mutex, see _MSIExecute Mutex."
    1619 = "installation package couldn't be opened. Verify that the package exists and is accessible, or contact the application vendor to verify that this is a valid Windows Installer package."
    1620 = "installation package couldn't be opened. Contact the application vendor to verify that this is a valid Windows Installer package."
    1621 = "was an error starting the Windows Installer service user interface. Contact your support personnel."
    1622 = "was an error opening installation log file. Verify that the specified log file location exists and is writable."
    1623 = "language of this installation package isn't supported by your system."
    1624 = "was an error applying transforms. Verify that the specified transform paths are valid."
    1625 = "installation is forbidden by system policy. Contact your system administrator."
    1626 = "function couldn't be executed."
    1627 = "function failed during execution."
    1628 = "invalid or unknown table was specified."
    1629 = "data supplied is the wrong type."
    1630 = "of this type isn't supported."
    1631 = "Windows Installer service failed to start. Contact your support personnel."
    1632 = "Temp folder is either full or inaccessible. Verify that the Temp folder exists and that you can write to it."
    1633 = "installation package isn't supported on this platform. Contact your application vendor."
    1634 = "isn't used on this machine."
    1635 = "patch package couldn't be opened. Verify that the patch package exists and is accessible, or contact the application vendor to verify that this is a valid Windows Installer patch package."
    1636 = "patch package couldn't be opened. Contact the application vendor to verify that this is a valid Windows Installer patch package."
    1637 = "patch package can't be processed by the Windows Installer service. You must install a Windows service pack that contains a newer version of the Windows Installer service."
    1638 = "version of this product is already installed. Installation of this version can't continue. To configure or remove the existing version of this product, use Add/Remove Programs in Control Panel."
    1639 = "command line argument. Consult the Windows Installer SDK for detailed command-line help."
    1640 = "current user isn't permitted to perform installations from a client session of a server running the Terminal Server role service."
    1641 = "installer has initiated a restart. This message indicates success."
    1642 = "installer can't install the upgrade patch because the program being upgraded may be missing or the upgrade patch updates a different version of the program. Verify that the program to be upgraded exists on your computer and that you have the correct upgrade patch."
    1643 = "patch package isn't permitted by system policy."
    1644 = "or more customizations aren't permitted by system policy."
    1645 = "Installer doesn't permit installation from a Remote Desktop Connection."
    1646 = "patch package isn't a removable patch package."
    1647 = "patch isn't applied to this product."
    1648 = "valid sequence could be found for the set of patches."
    1649 = "removal was disallowed by policy."
    1650 = "XML patch data is invalid."
    1651 = "user failed to apply patch for a per-user managed or a per-machine application that'is in advertised state."
    1652 = "Installer isn't accessible when the computer is in Safe Mode. Exit Safe Mode and try again or try using system restore to return your computer to a previous state. Available beginning with Windows Installer version 4.0."
    1653 = "t perform a multiple-package transaction because rollback has been disabled. Multiple-package installations can't run if rollback is disabled. Available beginning with Windows Installer version 4.5."
    1654 = "app that you're trying to run isn't supported on this version of Windows. A Windows Installer package, patch, or transform that has not been signed by Microsoft can't be installed on an ARM computer."
    3010 = "restart is required to complete the install. This message indicates success. This does not include installs where the ForceReboot action is run."
}