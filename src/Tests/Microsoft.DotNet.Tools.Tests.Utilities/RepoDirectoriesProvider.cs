// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public class RepoDirectoriesProvider
    {
        public RepoDirectoriesProvider(
            string artifacts = null,
            string nugetPackages = null,
            string pjDotnet = null)
        {
            Artifacts = artifacts ?? Path.Combine(RepoRoot, "artifacts", GetConfiguration());
            NugetPackages = nugetPackages ?? Path.Combine(RepoRoot, "artifacts", ".nuget", "packages");
            PjDotnet = pjDotnet ?? GetPjDotnetPath(Artifacts);
        }

        private static string s_repoRoot;
        private static string s_buildRid;
        public string Artifacts { get; }
        public string NugetPackages { get; }
        public string PjDotnet { get; }

        public static string RepoRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(s_repoRoot))
                {
                    return s_repoRoot;
                }

#if NET451
                string directory = AppDomain.CurrentDomain.BaseDirectory;
#else
                var directory = AppContext.BaseDirectory;
#endif
                while (!Directory.Exists(Path.Combine(directory, ".git")) && directory != null)
                {
                    directory = Directory.GetParent(directory).FullName;
                }

                s_repoRoot = directory ?? throw new Exception("Cannot find the git repository root");
                return s_repoRoot;
            }
        }

        private static string GetConfiguration()
        {
            // This is dependent on the current artifacts layout:
            // * $(RepoRoot)/artifacts/$(Configuration)/bin/Tests/$(MSBuildProjectName)
            return new DirectoryInfo(AppContext.BaseDirectory).Parent.Parent.Parent.Name;
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

        private static string GetPjDotnetPath(string artifacts)
        {
            var dotnetCliProjectJsonVersion = GetDotNetCliProjectJsonVersion(RepoRoot);

            return new DirectoryInfo(Path.Combine(RepoRoot, "artifacts", ".dotnet", dotnetCliProjectJsonVersion))
                .GetFiles("dotnet" + Constants.ExeSuffix).First()
                .FullName;
        }

        private static string GetDotNetCliProjectJsonVersion(string repoRoot)
        {
            var xml = XDocument.Load(Path.Combine(repoRoot, "build", "Versions.props"));

            foreach (var propertyGroup in xml.Descendants(XName.Get("PropertyGroup", "http://schemas.microsoft.com/developer/msbuild/2003")))
            {
                var dotnetCliVersion = propertyGroup.Descendants(XName.Get("DotNetCliProjectJsonVersion", "http://schemas.microsoft.com/developer/msbuild/2003"));

                if (dotnetCliVersion.Any())
                {
                    return dotnetCliVersion.Single().Value;
                }
            }

            throw new Exception("Failed to locate the .NET CLI Version");
        }
    }
}