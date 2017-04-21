// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using BuildCommand = Microsoft.DotNet.Tools.Test.Utilities.BuildCommand;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatIWantMigratedAppsToBinplaceContent : TestBase
    {
        [Fact(Skip = "Unblocking CI")]
        public void ItBinplacesContentOnBuildForConsoleApps()
        {
            var projectDirectory = TestAssets
                .GetProjectJson("TestAppWithContents")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .WithEmptyGlobalJson()
                .Root;

            new MigrateTestCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"{projectDirectory.FullName}")
                .Should()
                .Pass();


            var command = new RestoreCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute()
                .Should()
                .Pass();

            var result = new BuildCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput()
                .Should()
                .Pass();

            var outputDir = projectDirectory.GetDirectory("bin", "Debug", "netcoreapp1.0");
            outputDir.Should().Exist().And.HaveFile("testcontentfile.txt");
            outputDir.GetDirectory("dir").Should().Exist().And.HaveFile("mappingfile.txt");
        }

        [Fact(Skip = "Unblocking CI")]
        public void ItBinplacesContentOnPublishForConsoleApps()
        {
            var projectDirectory = TestAssets
                .GetProjectJson("TestAppWithContents")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .WithEmptyGlobalJson()
                .Root;

            new MigrateTestCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"{projectDirectory.FullName}")
                .Should()
                .Pass();

            var command = new RestoreCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute()
                .Should()
                .Pass();

            var result = new PublishCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput()
                .Should()
                .Pass();

            var publishDir = projectDirectory.GetDirectory("bin", "Debug", "netcoreapp1.0", "publish");
            publishDir.Should().Exist().And.HaveFile("testcontentfile.txt");
            publishDir.GetDirectory("dir").Should().Exist().And.HaveFile("mappingfile.txt");
        }

        [Fact(Skip = "CI does not have NPM, which is required for the publish of this app.")]
        public void ItBinplacesContentOnPublishForWebApps()
        {
            var projectDirectory = TestAssets
                .GetProjectJson("ProjectJsonWebTemplate")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles()
                .WithEmptyGlobalJson()
                .Root;

            new MigrateTestCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"{projectDirectory.FullName}")
                .Should()
                .Pass();

            var command = new RestoreCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute()
                .Should()
                .Pass();

            var result = new PublishCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput()
                .Should()
                .Pass();

            var publishDir = projectDirectory.GetDirectory("bin", "Debug", "netcoreapp1.0", "publish");
            publishDir.Should().Exist().And.HaveFile("README.md");
            publishDir.GetDirectory("wwwroot").Should().Exist();
        }
    }
}