set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
    DIR="$(cd -P "$(dirname "$SOURCE")" && pwd)"
    SOURCE="$(readlink "$SOURCE")"
    [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done

DIR="$(cd -P "$(dirname "$SOURCE")" && pwd)"
REPOROOT="$DIR"

# Some things depend on HOME and it may not be set. We should fix those things, but until then, we just patch a value in
if [ -z "$HOME" ]; then
    export HOME="$DIR/.home"

    [ ! -d "$HOME" ] || rm -Rf $HOME
    mkdir -p $HOME
fi

# $args array may have empty elements in it.
# The easiest way to remove them is to cast to string and back to array.
# This will actually break quoted arguments, arguments like 
# -test "hello world" will be broken into three arguments instead of two, as it should.
args=("$@")
temp="${args[@]}"
args=($temp)

export XDG_DATA_HOME="$REPOROOT/.nuget/packages"
export NUGET_PACKAGES="$REPOROOT/.nuget/packages"
export NUGET_HTTP_CACHE_PATH="$REPOROOT/.nuget/packages"
export DOTNET_INSTALL_DIR="$REPOROOT/.dotnet"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

ARCHITECTURE="x64"
# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
[ -z "$DOTNET_INSTALL_DIR_PJ" ] && export DOTNET_INSTALL_DIR_PJ=$REPOROOT/.dotnet_stage0PJ
[ -d "$DOTNET_INSTALL_DIR_PJ" ] || mkdir -p $DOTNET_INSTALL_DIR_PJ

# During xplat bootstrapping, disable HTTP parallelism to avoid fatal restore timeouts.
export __INIT_TOOLS_RESTORE_ARGS="$__INIT_TOOLS_RESTORE_ARGS --disable-parallel"

# Enable verbose VS Test Console logging
export VSTEST_BUILD_TRACE=1
export VSTEST_TRACE_BUILD=1

# args:
# remote_path - $1
# [out_path] - $2 - stdout if not provided
download() {
    eval $invocation

    local remote_path=$1
    local out_path=${2:-}

    local failed=false
    if [ -z "$out_path" ]; then
        curl --retry 10 -sSL --create-dirs $remote_path || failed=true
    else
        curl --retry 10 -sSL --create-dirs -o $out_path $remote_path || failed=true
    fi

    if [ "$failed" = true ]; then
        echo "run-build: Error: Download failed" >&2
        return 1
    fi
}

DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
toolsLocalPath="$REPOROOT/build_tools"
if [ ! -z $BOOTSTRAP_INSTALL_DIR]; then
    toolsLocalPath = $BOOTSTRAP_INSTALL_DIR
fi
bootStrapperPath="$toolsLocalPath/bootstrap.sh"
dotnetInstallPath="$toolsLocalPath/dotnet-install.sh"
if [ ! -f $bootStrapperPath ]; then
    if [ ! -d $toolsLocalPath ]; then
        mkdir $toolsLocalPath
    fi
    download "https://raw.githubusercontent.com/dotnet/buildtools/master/bootstrap/bootstrap.sh" "$bootStrapperPath"
    chmod u+x $bootStrapperPath
fi

$bootStrapperPath --dotNetInstallBranch master --repositoryRoot "$REPOROOT" --toolsLocalPath "$toolsLocalPath" --cliInstallPath $DOTNET_INSTALL_DIR_PJ --architecture $ARCHITECTURE >bootstrap.log
EXIT_CODE=$?
if [ $EXIT_CODE != 0 ]; then
    echo "run-build: Error: Boot-strapping failed with exit code $EXIT_CODE, see bootstrap.log for more information." >&2
    exit $EXIT_CODE
fi

# install dotnet cli latest master build
if [ ! -d "$DOTNET_INSTALL_DIR" ]; then
    mkdir $DOTNET_INSTALL_DIR
fi

curl -sSL https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh | bash /dev/stdin --install-dir $DOTNET_INSTALL_DIR --channel master -version 1.0.3

PATH="$DOTNET_INSTALL_DIR:$PATH"

dotnet msbuild build.proj /t:MakeVersionProps
dotnet msbuild build.proj /v:diag /fl /flp:v=diag "${args[@]}"
