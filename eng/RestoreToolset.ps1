Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Installs dotnet CLI used to restore project.json projects in tests.
function InitializeDotNetCli_ProjectJson {
  $dotnetRoot = Join-Path $RepoRoot ".dotnet-test"
  $sdkVersion = "1.0.0-preview2-1-003177"
  $sdkInstallDir = Join-Path $dotnetRoot "sdk\$sdkVersion"

  if (!(Test-Path $sdkInstallDir)) {
    InstallDotNetSdk $dotnetRoot $sdkVersion
  }
}

# Installs additional shared frameworks for testing purposes
function InstallDotNetSharedFramework([string]$dotnetRoot, [string]$version) {
  $fxDir = Join-Path $dotnetRoot "shared\Microsoft.NETCore.App\$version"

  if (!(Test-Path $fxDir)) {
    $installScript = GetDotNetInstallScript $dotnetRoot
    & $installScript -Version $version -InstallDir $dotnetRoot -SharedRuntime

    if($lastExitCode -ne 0) {
      throw "Failed to install shared Framework $version to '$dotnetRoot' (exit code '$lastExitCode')."
    }
  }
}

# The following frameworks and tools are used only for testing.
# Do not attempt to install them in source build.
if ($env:DotNetBuildFromSource -ne "true") {
  InitializeDotNetCli_ProjectJson
  InstallDotNetSharedFramework $env:DOTNET_INSTALL_DIR "1.0.5"
  InstallDotNetSharedFramework $env:DOTNET_INSTALL_DIR "1.1.1"
}