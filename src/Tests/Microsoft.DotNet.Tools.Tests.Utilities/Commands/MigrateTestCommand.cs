// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class MigrateTestCommand
    {
        private readonly StringBuilder _stdOut;
        private string _workingDirectory;

        public MigrateTestCommand()
        {
            _stdOut = new StringBuilder();
        }

        public CommandResult Execute(string args = "")
        {
            var resut = Migrate().Parse("migrate " + args);

            using (new WorkingDirectory(_workingDirectory))
            {
                var exitCode = resut["migrate"].Value<MigrateCommand.MigrateCommand>().Execute();
                return new CommandResult(
                    new ProcessStartInfo(), exitCode, _stdOut.ToString(), "");
            }
        }

        public MigrateTestCommand WithWorkingDirectory(DirectoryInfo withWorkingDirectory)
        {
            _workingDirectory = withWorkingDirectory.FullName;
            return this;
        }

        public MigrateTestCommand WithWorkingDirectory(string withWorkingDirectory)
        {
            _workingDirectory = withWorkingDirectory;
            return this;
        }

        public CommandResult ExecuteWithCapturedOutput(string args = "")
        {
            return Execute(args);
        }

        public Cli.CommandLine.Command Migrate() =>
            Create.Command(
                "migrate",
                ".NET Migrate Command",
                Accept.ZeroOrOneArgument()
                    .MaterializeAs(o =>
                        new MigrateCommand.MigrateCommand(
                            new CallStage0DotnetSlnToManipulateSolutionFile(),
                            new CallStage0DotnetNewToAddTemplate(),
                            o.ValueOrDefault<string>("--template-file"),
                            o.Arguments.FirstOrDefault(),
                            o.ValueOrDefault<string>("--sdk-package-version"),
                            o.ValueOrDefault<string>("--xproj-file"),
                            o.ValueOrDefault<string>("--report-file"),
                            o.ValueOrDefault<bool>("--skip-project-references"),
                            o.ValueOrDefault<bool>("--format-report-file-json"),
                            o.ValueOrDefault<bool>("--skip-backup"), (l) => _stdOut.Append(l)))
                    .With(name: "",
                        description: ""),
                Create.Option("-t|--template-file",
                    "",
                    Accept.ExactlyOneArgument()),
                Create.Option("-v|--sdk-package-version",
                    "",
                    Accept.ExactlyOneArgument()),
                Create.Option("-x|--xproj-file",
                    "",
                    Accept.ExactlyOneArgument()),
                Create.Option("-s|--skip-project-references",
                    ""),
                Create.Option("-r|--report-file",
                    "",
                    Accept.ExactlyOneArgument()),
                Create.Option("--format-report-file-json",
                    ""),
                Create.Option("--skip-backup",
                    ""));

        public class WorkingDirectory : IDisposable
        {
            private readonly string _backUpCurrentDirectory;

            public WorkingDirectory(string workingDirectory)
            {
                var workingDirectory1 = workingDirectory;
                _backUpCurrentDirectory = Directory.GetCurrentDirectory();

                if (workingDirectory1 != null)
                {
                    Directory.SetCurrentDirectory(workingDirectory1);
                }
            }

            public void Dispose()
            {
                Directory.SetCurrentDirectory(_backUpCurrentDirectory);
            }
        }
    }
}