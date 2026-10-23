#Requires -Version 5.1
<#
.SYNOPSIS
Builds and installs SlnDropBuilder for the current Windows user.
.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\install.ps1
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\SlnDropBuilder'),
    [switch] $BuildOnly
)

$ErrorActionPreference = 'Stop'
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$workDirectory = Join-Path $temporaryRoot ('SlnDropBuilder-install-' + [guid]::NewGuid().ToString('N'))
$publishDirectory = Join-Path $workDirectory 'publish'
$logPath = $workDirectory + '.log'
$transcribing = $false
$locationPushed = $false
$stage = 'Validate source and destination'
try {
    if (-not $WhatIfPreference) {
        Start-Transcript -Path $logPath -Force | Out-Null
        $transcribing = $true
        Write-Host "Log: $logPath"
    }
    if ($env:OS -ne 'Windows_NT' -or -not [Environment]::Is64BitOperatingSystem) {
        throw 'SlnDropBuilder requires 64-bit Windows.'
    }
    $project = Join-Path $PSScriptRoot 'SlnDropBuilder.csproj'
    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
        throw 'Project source files are missing. Extract the entire repository; the PowerShell files alone cannot build SlnDropBuilder.'
    }
    $InstallDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($InstallDirectory)
    $action = if ($BuildOnly) { 'Install missing .NET SDK and publish SlnDropBuilder build files' } else { 'Install missing .NET SDK, build and install SlnDropBuilder, and create a Start menu shortcut' }
    if (-not $PSCmdlet.ShouldProcess($InstallDirectory, $action)) {
        return
    }
    $destinationExecutable = Join-Path $InstallDirectory 'SlnDropBuilder.exe'
    $runningDestination = Get-Process -Name 'SlnDropBuilder' -ErrorAction SilentlyContinue | Where-Object {
        -not $_.Path -or [string]::Equals($_.Path, $destinationExecutable, [StringComparison]::OrdinalIgnoreCase)
    }
    if ($runningDestination) {
        throw "Close SlnDropBuilder before updating '$InstallDirectory'."
    }
    # SDK selection and NuGet configuration must not depend on the caller's folder.
    Push-Location -LiteralPath $PSScriptRoot
    $locationPushed = $true

    $stage = 'Find a usable .NET SDK'
    $sdkDirectory = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'
    $sdkExecutable = Join-Path $sdkDirectory 'dotnet.exe'
    $dotnet = $null
    foreach ($candidate in @('dotnet', $sdkExecutable)) {
        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if (-not $command) { continue }
        try {
            $sdks = @(& $command --list-sdks 2>$null)
            if ($LASTEXITCODE -eq 0 -and ($sdks -match '^(8|9|[1-9][0-9]+)\.')) {
                $dotnet = $command
                break
            }
        }
        catch { Write-Verbose "Ignoring unusable .NET host: $candidate. $_" }
    }
    if (-not $dotnet) {
        $stage = 'Download the .NET SDK installer'
        Write-Host 'Installing .NET SDK 8 for the current user...'
        New-Item -ItemType Directory -Path $workDirectory -Force | Out-Null
        $sdkInstaller = Join-Path $workDirectory 'dotnet-install.ps1'
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $installerUrls = @('https://dot.net/v1/dotnet-install.ps1', 'https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1')
        foreach ($url in $installerUrls) {
            try {
                Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 60 -OutFile $sdkInstaller
                break
            }
            catch {
                if ($url -eq $installerUrls[-1]) { throw }
                Write-Warning "Download failed from $url. Trying Microsoft's alternate endpoint. $_"
            }
        }
        $stage = 'Install .NET SDK 8'
        # The official installer can call exit, so run it in a child process.
        # Omitting Quality also allows its latest.version fallback when aka.ms is blocked.
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $sdkInstaller -Channel 8.0 -Architecture x64 -InstallDir $sdkDirectory -NoPath
        if ($LASTEXITCODE -ne 0) {
            throw ".NET SDK installation failed (exit code $LASTEXITCODE)."
        }
        $dotnet = Get-Command $sdkExecutable -ErrorAction Stop
        $sdks = @(& $dotnet --list-sdks)
        if ($LASTEXITCODE -ne 0 -or -not ($sdks -match '^8\.')) {
            throw '.NET SDK installation did not produce a usable .NET 8 SDK.'
        }
    }

    $stage = 'Restore NuGet packages and publish SlnDropBuilder'
    & $dotnet publish $project --configuration Release --runtime win-x64 --self-contained true --output $publishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "SlnDropBuilder publish failed (exit code $LASTEXITCODE)."
    }

    if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory 'SlnDropBuilder.exe') -PathType Leaf)) {
        throw 'Publish did not produce SlnDropBuilder.exe. Destination was not changed.'
    }

    $stage = 'Copy build files to destination'
    New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $publishDirectory -Force |
        Copy-Item -Destination $InstallDirectory -Recurse -Force

    if ($BuildOnly) {
        Write-Host "SlnDropBuilder build files: $InstallDirectory"
        Write-Host 'Run SlnDropBuilder.exe from this folder. Copy the entire folder when distributing the app.'
    }
    else {
        $stage = 'Create Start menu shortcut'
        $programs = [Environment]::GetFolderPath('Programs')
        New-Item -ItemType Directory -Path $programs -Force | Out-Null
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut((Join-Path $programs 'SlnDropBuilder.lnk'))
        $shortcut.TargetPath = Join-Path $InstallDirectory 'SlnDropBuilder.exe'
        $shortcut.WorkingDirectory = $InstallDirectory
        $shortcut.IconLocation = $shortcut.TargetPath + ',0'
        $shortcut.Save()

        Write-Host "SlnDropBuilder installed: $InstallDirectory"
        Write-Host 'Launch SlnDropBuilder from the Start menu. A .NET SDK is still required to build other projects.'
    }
}
catch {
    Write-Host "Failed step: $stage" -ForegroundColor Red
    Write-Host $_.ToString() -ForegroundColor Red
    Write-Host 'For download/restore errors, check Internet access, proxy settings, and trusted certificates for dot.net, aka.ms, builds.dotnet.microsoft.com and api.nuget.org.'
    if ($transcribing) { Write-Host "Full log: $logPath" }
    throw
}
finally {
    try {
        if (Test-Path -LiteralPath $workDirectory) {
            $cleanupPath = (Resolve-Path -LiteralPath $workDirectory).ProviderPath
            if (-not $cleanupPath.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Unexpected temporary directory: $cleanupPath"
            }
            Remove-Item -LiteralPath $cleanupPath -Recurse -Force
        }
    }
    catch { Write-Warning "Could not clean temporary files: $_" }
    if ($locationPushed) { Pop-Location }
    if ($transcribing) { Stop-Transcript | Out-Null }
}
