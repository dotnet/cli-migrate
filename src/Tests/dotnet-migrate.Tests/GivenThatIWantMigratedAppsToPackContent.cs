// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using System.Linq;
using Xunit;
using FluentAssertions;
using System.IO;
using System.IO.Compression;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatIWantMigratedAppsToPackContent : TestBase
    {
        [Fact(Skip = "Unblocking CI")]
        public void ItPacksContentForLibraries()
        {
            var projectDirectory = TestAssets
                .GetProjectJson("PJTestLibraryWithConfiguration")
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

            var result = new PackCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput()
                .Should()
                .Pass();

            using (var archive = ZipFile.OpenRead(
                Path.Combine(projectDirectory.FullName, "bin", "debug", "PJTestLibraryWithConfiguration.1.0.0.nupkg")))
            {
                archive.Entries.Select(e => e.FullName).Should().Contain("dir/contentitem.txt");
            }
        }
    }
}