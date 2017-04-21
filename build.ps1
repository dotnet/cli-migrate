[cmdletbinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Architecture = "x64",
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    $ExtraParameters
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$RepoRoot = "$PSScriptRoot"
$DOTNET_INSTALL_DIR = "$REPOROOT/.dotnet"

$env:XDG_DATA_HOME = "$REPOROOT/.nuget/packages"
$env:NUGET_PACKAGES = "$REPOROOT/.nuget/packages"
$env:NUGET_HTTP_CACHE_PATH = "$REPOROOT/.nuget/packages"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
if (!$env:DOTNET_INSTALL_DIR_PJ) {
    $env:DOTNET_INSTALL_DIR_PJ = "$RepoRoot\.dotnet_stage0PJ"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR_PJ)) {
    mkdir $env:DOTNET_INSTALL_DIR_PJ | Out-Null
}

# Disable first run since we want to control all package sources
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1

# Enable vs test console logging
$env:VSTEST_BUILD_TRACE = 1
$env:VSTEST_TRACE_BUILD = 1

# set the base tools directory
$toolsLocalPath = Join-Path $PSScriptRoot "build_tools"
if ($env:BOOTSTRAP_INSTALL_DIR) {
    $toolsLocalPath = $env:BOOTSTRAP_INSTALL_DIR
}
$bootStrapperPath = Join-Path $toolsLocalPath "bootstrap.ps1"
# if the boot-strapper script doesn't exist then download it
if ((Test-Path $bootStrapperPath) -eq 0) {
    if ((Test-Path $toolsLocalPath) -eq 0) {
        mkdir $toolsLocalPath | Out-Null
    }

    # download boot-strapper script
    Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/buildtools/master/bootstrap/bootstrap.ps1" -OutFile $bootStrapperPath
}

# now execute it
& $bootStrapperPath -DotNetInstallBranch master -RepositoryRoot (Get-Location) -ToolsLocalPath $toolsLocalPath -CliLocalPath $env:DOTNET_INSTALL_DIR_PJ -Architecture $Architecture | Out-File (Join-Path (Get-Location) "bootstrap.log")
if ($LastExitCode -ne 0) {
    Write-Output "Boot-strapping failed with exit code $LastExitCode, see bootstrap.log for more information."
    exit $LastExitCode
}

# install dotnet cli latest master build
if (-Not (Test-Path $DOTNET_INSTALL_DIR)) {
    New-Item -Type "directory" -Path $DOTNET_INSTALL_DIR 
}

Invoke-WebRequest -Uri "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1" -OutFile "$DOTNET_INSTALL_DIR/dotnet-install.ps1"
& $DOTNET_INSTALL_DIR/dotnet-install.ps1 -Channel "master" -InstallDir "$DOTNET_INSTALL_DIR" -Version 1.0.3

$env:PATH = "$DOTNET_INSTALL_DIR;$env:PATH"

& dotnet msbuild build.proj /t:MakeVersionProps
& dotnet msbuild build.proj /v:diag /fl /flp:v=diag $ExtraParameters
