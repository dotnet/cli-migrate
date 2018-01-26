// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Tools.MigrateCommand;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public class CallStage0DotnetSlnToManipulateSolutionFile : ICanManipulateSolutionFile
    {
        public void AddProjectToSolution(string solutionFilePath, string projectFilePath)
        {
            var exitCode = new DotnetCommand().Execute($"sln {solutionFilePath} add {projectFilePath}").ExitCode;

            if (exitCode != 0)
            {
                throw new InvalidOperationException();
            }
        }

        public void RemoveProjectFromSolution(string solutionFilePath, string projectFilePath)
        {
            var exitCode = new DotnetCommand().Execute($"sln {solutionFilePath} remove {projectFilePath}").ExitCode;

            if (exitCode != 0)
            {
                throw new InvalidOperationException();
            }
        }
    }
}