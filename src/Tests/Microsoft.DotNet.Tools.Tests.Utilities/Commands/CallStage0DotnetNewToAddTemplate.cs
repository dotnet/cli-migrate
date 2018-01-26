// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Tools.MigrateCommand;

namespace Microsoft.DotNet.Tools.Test.Utilities
{

    public class CallStage0DotnetNewToAddTemplate : ICanCreateDotnetCoreTemplate
    {
        public void CreateWithEphemeralHiveAndNoRestore(
            string templateName,
            string outputDirectory,
            string workingDirectory)
        {
            var result = new NewCommand()
                .WithWorkingDirectory(workingDirectory)
                .Execute($"{templateName} -o {outputDirectory} --debug:ephemeral-hive");

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(result.StdErr);
            }
        }
    }
}