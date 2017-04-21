// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Tools.Tests.Utilities
{
    public class GetCurrentRuntimeInformation
    {
        public GetCurrentRuntimeInformation()
        {
            Rid = RuntimeEnvironment.GetRuntimeIdentifier();
            OSName = GetOSShortName();
            OSPlatform = RuntimeEnvironment.OperatingSystemPlatform.ToString().ToLower();
        }

        public string Rid { get; }
        public string OSName { get; }
        public string OSPlatform { get; }

        private static string GetOSShortName()
        {
            string osname = "";
            switch (CurrentPlatform.Current)
            {
                case BuildPlatform.Windows:
                    osname = "win";
                    break;
                default:
                    osname = CurrentPlatform.Current.ToString().ToLower();
                    break;
            }

            return osname;
        }
    }
}
