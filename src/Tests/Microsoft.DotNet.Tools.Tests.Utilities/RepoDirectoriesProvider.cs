// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public static class RepoDirectoriesProvider
    {
        private static string s_buildRid;

        public static string TestAssetsRoot { get; }
        public static string DotNetProjectJsonPath { get; }

        static RepoDirectoriesProvider()
        {
            var attribute = typeof(RepoDirectoriesProvider).GetTypeInfo().Assembly.GetCustomAttribute<RepoDirectoriesAttribute>();
            TestAssetsRoot = Path.Combine(attribute.RepoRoot, "src", "Assets");
            DotNetProjectJsonPath = Path.Combine(attribute.RepoRoot, ".dotnet-test", "dotnet" + Constants.ExeSuffix);
        }

        public static string BuildRid
        {
            get
            {
                if (!string.IsNullOrEmpty(s_buildRid)) return s_buildRid;
                s_buildRid = RuntimeEnvironment.GetRuntimeIdentifier();
                if (string.IsNullOrEmpty(s_buildRid))
                {
                    throw new InvalidOperationException($"Could not find a property named 'Rid'");
                }
                return s_buildRid;
            }
        }
    }
}