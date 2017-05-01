// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Build.Construction;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectJsonMigration;
using Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.Tools.MigrateCommand
{
    internal class TemporaryDotnetNewTemplateProject
    {
        private const string c_temporaryDotnetNewMSBuildProjectName = "p";
        private readonly string _projectDirectory;
        private readonly ICanCreateDotnetCoreTemplate _dotnetCoreTemplateCreator;

        public ProjectRootElement MSBuildProject { get; }

        public string MSBuildProjectPath => Path.Combine(_projectDirectory,
            c_temporaryDotnetNewMSBuildProjectName + ".csproj");

        public TemporaryDotnetNewTemplateProject(ICanCreateDotnetCoreTemplate dotnetCoreTemplateCreator)
        {
            if (dotnetCoreTemplateCreator == null)
            {
                throw new ArgumentNullException(nameof(dotnetCoreTemplateCreator));
            }
            _dotnetCoreTemplateCreator = dotnetCoreTemplateCreator;

            _projectDirectory = CreateDotnetNewMSBuild(c_temporaryDotnetNewMSBuildProjectName);
            MSBuildProject = GetMSBuildProject();

        }

        public void Clean()
        {
            Directory.Delete(Path.Combine(_projectDirectory, ".."), true);
        }

        private string CreateDotnetNewMSBuild(string projectName)
        {
            var tempDir = Path.Combine(
                Path.GetTempPath(),
                this.GetType().Namespace,
                Path.GetRandomFileName(),
                c_temporaryDotnetNewMSBuildProjectName);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            _dotnetCoreTemplateCreator.CreateWithEphemeralHiveAndNoRestore("console", tempDir, tempDir);

            return tempDir;
        }

        private ProjectRootElement GetMSBuildProject()
        {
            return ProjectRootElement.Open(
                MSBuildProjectPath,
                ProjectCollection.GlobalProjectCollection,
                preserveFormatting: true);
        }
    }
}