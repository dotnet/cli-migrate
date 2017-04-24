// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ProjectJsonMigration.SolutionFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Tools.MigrateCommand
{
    internal static class SlnFileExtensions
    {
        public static void RemoveEmptySolutionFolders(this SlnFile slnFile)
        {
            var solutionFolderProjects = slnFile.Projects
                .GetProjectsByType(ProjectTypeGuids.SolutionFolderGuid)
                .ToList();

            if (solutionFolderProjects.Any())
            {
                var nestedProjectsSection = slnFile.Sections.GetSection(
                    "NestedProjects",
                    SlnSectionType.PreProcess);

                if (nestedProjectsSection == null)
                {
                    foreach (var solutionFolderProject in solutionFolderProjects)
                    {
                        if (solutionFolderProject.Sections.Count() == 0)
                        {
                            slnFile.Projects.Remove(solutionFolderProject);
                        }
                    }
                }
                else
                {
                    var solutionFoldersInUse = slnFile.GetSolutionFoldersThatContainProjectsInItsHierarchy(
                        nestedProjectsSection.Properties);

                    foreach (var solutionFolderProject in solutionFolderProjects)
                    {
                        if (!solutionFoldersInUse.Contains(solutionFolderProject.Id))
                        {
                            nestedProjectsSection.Properties.Remove(solutionFolderProject.Id);
                            if (solutionFolderProject.Sections.Count() == 0)
                            {
                                slnFile.Projects.Remove(solutionFolderProject);
                            }
                        }
                    }

                    if (nestedProjectsSection.IsEmpty)
                    {
                        slnFile.Sections.Remove(nestedProjectsSection);
                    }
                }
            }
        }

        private static HashSet<string> GetSolutionFoldersThatContainProjectsInItsHierarchy(
            this SlnFile slnFile,
            SlnPropertySet nestedProjects)
        {
            var solutionFoldersInUse = new HashSet<string>();

            var nonSolutionFolderProjects = slnFile.Projects.GetProjectsNotOfType(
                ProjectTypeGuids.SolutionFolderGuid);

            foreach (var nonSolutionFolderProject in nonSolutionFolderProjects)
            {
                var id = nonSolutionFolderProject.Id;
                while (nestedProjects.ContainsKey(id))
                {
                    id = nestedProjects[id];
                    solutionFoldersInUse.Add(id);
                }
            }

            return solutionFoldersInUse;
        }
    }
}
