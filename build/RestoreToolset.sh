# Installs dotnet CLI used to restore project.json projects in tests.
function InitializeDotNetCli_ProjectJson {
  local dotnet_root="$repo_root/.dotnet-test"
  local sdk_version="1.0.0-preview2-1-003177"
  local sdk_install_dir="$dotnet_root/sdk/$sdk_version"

  if [[ ! -d "$sdk_install_dir" ]]; then
    InstallDotNetSdk $dotnet_root $sdk_version
  fi
}

# Installs additional shared frameworks for testing purposes
function InstallDotNetSharedFramework {
  local dotnet_root=$1
  local version=$2
  local fx_dir="$dotnet_root/shared/Microsoft.NETCore.App/$version"

  if [[ ! -d "$fx_dir" ]]; then
    local install_script=`GetDotNetInstallScript $dotnet_root`
    
    bash "$install_script" --version $version --install-dir $dotnet_root --shared-runtime
    local lastexitcode=$?
    
    if [[ $lastexitcode != 0 ]]; then
      echo "Failed to install Shared Framework $version to '$dotnet_root' (exit code '$lastexitcode')."
      ExitWithExitCode $lastexitcode
    fi
  fi
}

# The following frameworks and tools are used only for testing.
# Do not attempt to install them in source build.
if [[ "$DotNetBuildFromSource" != "true" ]]; then
  InitializeDotNetCli_ProjectJson
  InstallDotNetSharedFramework $DOTNET_INSTALL_DIR "1.0.5"
  InstallDotNetSharedFramework $DOTNET_INSTALL_DIR "1.1.1"
fi