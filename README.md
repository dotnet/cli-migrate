# CLI `dotnet migrate` command implementation

This repository contains the code for the `dotnet migrate` command that was previously under [dotnet/cli](https://github.com/dotnet/cli), and that migrates "project.json" based projects to "csproj" based projects. Please see the [public documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-migrate) for more information.

This command was removed in .NET SDK 3.0 so only applies to 2.2 and earlier releases. This repo has been archived since 2.1 SDKs are seeing minimal changes and will go [out of support](https://devblogs.microsoft.com/dotnet/net-core-2-1-will-reach-end-of-support-on-august-21-2021/) in 6 months.

## Build Status

| Latest Daily Build<br>*master* |
|:------:|
| [![][win-x64-build-badge]][win-x64-build] |

[win-x64-build-badge]: https://dnceng.visualstudio.com/internal/_apis/build/status/dotnet/cli-migrate/cli-migrate%203.0%20(Windows)%20(YAML)%20(Official)
[win-x64-build]: https://dnceng.visualstudio.com/internal/_build?definitionId=142

## Using CLI `dotnet migrate`

You can simply `git clone` this project to get started. It is recommended that you don't preserve history of the project (it isn't generally meaningful) for your repo, but make a copy and `git init` your project from source.

## Building

[to be completed].

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on contributing to this project.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## License

This project is licensed with the [MIT license](LICENSE).

## .NET Foundation

New Repo is a [.NET Foundation project](https://dotnetfoundation.org/projects).

## Related Projects

You should take a look at these related projects:

- [.NET Core CLI](https://github.com/dotnet/cli)
- [.NET Core](https://github.com/dotnet/core)
- [ASP.NET](https://github.com/aspnet)
- [Mono](https://github.com/mono)
