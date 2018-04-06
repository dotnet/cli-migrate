using System;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    [AttributeUsage(AttributeTargets.Assembly)]
    internal class RepoDirectoriesAttribute : Attribute
    {
        public string RepoRoot { get; }

        public RepoDirectoriesAttribute(string repoRoot)
        {
            RepoRoot = repoRoot;
        }
    }
}
