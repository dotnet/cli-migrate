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
  local version=$1
  local dotnet_root=$DOTNET_INSTALL_DIR
  local fx_dir="$dotnet_root/shared/Microsoft.NETCore.App/$version"

  if [[ ! -d "$fx_dir" ]]; then
    GetDotNetInstallScript "$dotnet_root"
    local install_script=$_GetDotNetInstallScript

    bash "$install_script" --version $version --install-dir "$dotnet_root" --runtime "dotnet" || true
  fi
}

# The following frameworks and tools are used only for testing.
# Do not attempt to install them in source build.
if [[ "${DotNetBuildFromSource:-}" == "true" ]]; then
  return
fi

InitializeDotNetCli true
InitializeDotNetCli_ProjectJson
InstallDotNetSharedFramework "1.0.5"
InstallDotNetSharedFramework "1.1.1"