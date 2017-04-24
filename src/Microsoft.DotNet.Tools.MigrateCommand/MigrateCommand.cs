﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.ProjectJsonMigration.SolutionFile;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectJsonMigration;
using Microsoft.DotNet.Internal.ProjectModel;
using Project = Microsoft.DotNet.Internal.ProjectModel.Project;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.Tools.MigrateCommand
{
    public partial class MigrateCommand
    {
        private const string ProductDescription = "Visual Studio 15";
        private const string VisualStudioVersion = "15.0.26114.2";
        private const string MinimumVisualStudioVersion = "10.0.40219.1";

        private SlnFile _slnFile;
        private readonly DirectoryInfo _workspaceDirectory;
        private readonly string _templateFile;
        private readonly string _projectArg;
        private readonly string _sdkVersion;
        private readonly string _xprojFilePath;
        private readonly bool _skipProjectReferences;
        private readonly string _reportFile;
        private readonly bool _reportFormatJson;
        private readonly bool _skipBackup;
        private readonly ICanManipulateSolutionFile _solutionFileManipulator;
        private readonly ICanCreateDotnetCoreTemplate _dotnetCoreTemplateCreator;
        private readonly Action<string> _reporterWriteLine;

        public MigrateCommand(
            ICanManipulateSolutionFile solutionFileManipulator,
            ICanCreateDotnetCoreTemplate dotnetCoreTemplateCreator,
            string templateFile,
            string projectArg,
            string sdkVersion,
            string xprojFilePath,
            string reportFile,
            bool skipProjectReferences,
            bool reportFormatJson,
            bool skipBackup,
            Action<string> reporterWriteLine = null
        )
        {
            if (solutionFileManipulator == null)
            {
                throw new ArgumentNullException(nameof(solutionFileManipulator));
            }
            if (dotnetCoreTemplateCreator == null)
            {
                throw new ArgumentNullException(nameof(dotnetCoreTemplateCreator));
            }

            _solutionFileManipulator = solutionFileManipulator;
            _dotnetCoreTemplateCreator = dotnetCoreTemplateCreator;
            _templateFile = templateFile;
            _projectArg = projectArg ?? Directory.GetCurrentDirectory();
            _workspaceDirectory = File.Exists(_projectArg)
                ? new FileInfo(_projectArg).Directory
                : new DirectoryInfo(_projectArg);
            _sdkVersion = sdkVersion;
            _xprojFilePath = xprojFilePath;
            _skipProjectReferences = skipProjectReferences;
            _reportFile = reportFile;
            _reportFormatJson = reportFormatJson;
            _skipBackup = skipBackup;
            _reporterWriteLine = reporterWriteLine ?? Reporter.Output.WriteLine;
        }

        public int Execute()
        {
            var temporaryDotnetNewProject = new TemporaryDotnetNewTemplateProject(_dotnetCoreTemplateCreator);
            var projectsToMigrate = GetProjectsToMigrate(_projectArg);

            var msBuildTemplatePath = _templateFile ?? temporaryDotnetNewProject.MSBuildProjectPath;

            MigrationReport migrationReport = null;

            foreach (var project in projectsToMigrate)
            {
                var projectDirectory = Path.GetDirectoryName(project);
                var outputDirectory = projectDirectory;
                var migrationSettings = new MigrationSettings(
                    projectDirectory,
                    outputDirectory,
                    msBuildTemplatePath,
                    _xprojFilePath,
                    null,
                    _slnFile);
                var projectMigrationReport = new ProjectMigrator().Migrate(migrationSettings, _skipProjectReferences);

                if (migrationReport == null)
                {
                    migrationReport = projectMigrationReport;
                }
                else
                {
                    migrationReport = migrationReport.Merge(projectMigrationReport);
                }
            }

            WriteReport(migrationReport);

            temporaryDotnetNewProject.Clean();

            UpdateSolutionFile(migrationReport);

            MoveProjectJsonArtifactsToBackup(migrationReport);

            return migrationReport.FailedProjectsCount;
        }

        private void UpdateSolutionFile(MigrationReport migrationReport)
        {
            if (_slnFile != null)
            {
                UpdateSolutionFile(migrationReport, _slnFile);
            }
            else
            {
                foreach (var slnPath in _workspaceDirectory.EnumerateFiles("*.sln"))
                {
                    var slnFile = SlnFile.Read(slnPath.FullName);

                    UpdateSolutionFile(migrationReport, slnFile);
                }
            }
        }

        private void UpdateSolutionFile(MigrationReport migrationReport, SlnFile slnFile)
        {
            if (slnFile == null)
            {
                return;
            }

            if (migrationReport.FailedProjectsCount > 0)
            {
                return;
            }

            var csprojFilesToAdd = new HashSet<string>();
            var xprojFilesToRemove = new HashSet<string>();

            var slnPathWithTrailingSlash = PathUtility.EnsureTrailingSlash(slnFile.BaseDirectory);
            foreach (var report in migrationReport.ProjectMigrationReports)
            {
                var reportPathWithTrailingSlash = PathUtility.EnsureTrailingSlash(report.ProjectDirectory);
                var relativeReportPath = PathUtility.GetRelativePath(
                    slnPathWithTrailingSlash,
                    reportPathWithTrailingSlash);

                var migratedProjectName = report.ProjectName + ".csproj";
                var csprojPath = Path.Combine(relativeReportPath, migratedProjectName);
                var solutionContainsCsprojPriorToMigration = slnFile
                    .Projects
                    .Any(p => p.FilePath == csprojPath);

                if (!solutionContainsCsprojPriorToMigration)
                {
                    csprojFilesToAdd.Add(Path.Combine(report.ProjectDirectory, migratedProjectName));
                }

                foreach (var preExisting in report.PreExistingCsprojDependencies)
                {
                    csprojFilesToAdd.Add(Path.Combine(report.ProjectDirectory, preExisting));
                }

                var projectDirectory = new DirectoryInfo(report.ProjectDirectory);
                foreach (var xprojFileName in projectDirectory.EnumerateFiles("*.xproj"))
                {
                    var xprojPath = Path.Combine(relativeReportPath, xprojFileName.Name);
                    var solutionContainsXprojFileToRemove = slnFile
                        .Projects
                        .Any(p => p.FilePath == xprojPath);

                    if (solutionContainsXprojFileToRemove)
                    {
                        xprojFilesToRemove.Add(Path.Combine(report.ProjectDirectory, xprojFileName.Name));
                    }
                }
            }

            Version version;
            if (!Version.TryParse(slnFile.VisualStudioVersion, out version) || version.Major < 15)
            {
                slnFile.ProductDescription = ProductDescription;
                slnFile.VisualStudioVersion = VisualStudioVersion;
                slnFile.MinimumVisualStudioVersion = MinimumVisualStudioVersion;
            }

            RemoveReferencesToMigratedFiles(slnFile);

            slnFile.Write();

            foreach (var csprojFile in csprojFilesToAdd)
            {
                _solutionFileManipulator.AddProjectToSolution(slnFile.FullPath, csprojFile);
            }

            foreach (var xprojFile in xprojFilesToRemove)
            {
                _solutionFileManipulator.RemoveProjectFromSolution(slnFile.FullPath, xprojFile);
            }
        }

        private void RemoveReferencesToMigratedFiles(SlnFile slnFile)
        {
            var solutionFolders = slnFile.Projects.GetProjectsByType(ProjectTypeGuids.SolutionFolderGuid);

            foreach (var solutionFolder in solutionFolders)
            {
                var solutionItems = solutionFolder.Sections.GetSection("SolutionItems");
                if (solutionItems != null && solutionItems.Properties.ContainsKey("global.json"))
                {
                    solutionItems.Properties.Remove("global.json");
                    if (solutionItems.IsEmpty)
                    {
                        solutionFolder.Sections.Remove(solutionItems);
                    }
                }
            }

            slnFile.RemoveEmptySolutionFolders();
        }

        private void MoveProjectJsonArtifactsToBackup(MigrationReport migrationReport)
        {
            if (_skipBackup)
            {
                return;
            }

            if (migrationReport.FailedProjectsCount > 0)
            {
                return;
            }

            BackupProjects(migrationReport);
        }

        private void BackupProjects(MigrationReport migrationReport)
        {
            var projectDirectories = new List<DirectoryInfo>();
            foreach (var report in migrationReport.ProjectMigrationReports)
            {
                projectDirectories.Add(new DirectoryInfo(report.ProjectDirectory));
            }

            var backupPlan = new MigrationBackupPlan(
                projectDirectories,
                _workspaceDirectory);

            backupPlan.PerformBackup();

            _reporterWriteLine(string.Format(
                LocalizableStrings.MigrateFilesBackupLocation,
                backupPlan.RootBackupDirectory.FullName));
        }

        private void WriteReport(MigrationReport migrationReport)
        {
            if (!string.IsNullOrEmpty(_reportFile))
            {
                using (var outputTextWriter = GetReportFileOutputTextWriter())
                {
                    outputTextWriter.Write(GetReportContent(migrationReport));
                }
            }

            WriteReportToStdOut(migrationReport);
        }

        private void WriteReportToStdOut(MigrationReport migrationReport)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var projectMigrationReport in migrationReport.ProjectMigrationReports)
            {
                var errorContent = GetProjectReportErrorContent(projectMigrationReport, colored: true);
                var successContent = GetProjectReportSuccessContent(projectMigrationReport, colored: true);
                var warningContent = GetProjectReportWarningContent(projectMigrationReport, colored: true);
                _reporterWriteLine(warningContent);
                if (!string.IsNullOrEmpty(errorContent))
                {
                    Reporter.Error.WriteLine(errorContent);
                }
                else
                {
                    _reporterWriteLine(successContent);
                }
            }

            _reporterWriteLine(GetReportSummary(migrationReport));

            _reporterWriteLine(LocalizableStrings.MigrationAdditionalHelp);
        }

        private string GetReportContent(MigrationReport migrationReport, bool colored = false)
        {
            if (_reportFormatJson)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(migrationReport);
            }

            StringBuilder sb = new StringBuilder();

            foreach (var projectMigrationReport in migrationReport.ProjectMigrationReports)
            {
                var errorContent = GetProjectReportErrorContent(projectMigrationReport, colored: colored);
                var successContent = GetProjectReportSuccessContent(projectMigrationReport, colored: colored);
                var warningContent = GetProjectReportWarningContent(projectMigrationReport, colored: colored);
                sb.AppendLine(warningContent);
                if (!string.IsNullOrEmpty(errorContent))
                {
                    sb.AppendLine(errorContent);
                }
                else
                {
                    sb.AppendLine(successContent);
                }
            }

            sb.AppendLine(GetReportSummary(migrationReport));

            return sb.ToString();
        }

        private string GetReportSummary(MigrationReport migrationReport)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(LocalizableStrings.MigrationReportSummary);
            sb.AppendLine(
                string.Format(LocalizableStrings.MigrationReportTotalProjects, migrationReport.MigratedProjectsCount));
            sb.AppendLine(string.Format(
                LocalizableStrings.MigrationReportSucceededProjects,
                migrationReport.SucceededProjectsCount));
            sb.AppendLine(string.Format(
                LocalizableStrings.MigrationReportFailedProjects,
                migrationReport.FailedProjectsCount));

            return sb.ToString();
        }

        private string GetProjectReportSuccessContent(ProjectMigrationReport projectMigrationReport, bool colored)
        {
            Func<string, string> GreenIfColored = (str) => colored ? str.Green() : str;
            return GreenIfColored(string.Format(
                LocalizableStrings.ProjectMigrationSucceeded,
                projectMigrationReport.ProjectName,
                projectMigrationReport.ProjectDirectory));
        }

        private string GetProjectReportWarningContent(ProjectMigrationReport projectMigrationReport, bool colored)
        {
            StringBuilder sb = new StringBuilder();
            Func<string, string> YellowIfColored = (str) => colored ? str.Yellow() : str;

            foreach (var warning in projectMigrationReport.Warnings)
            {
                sb.AppendLine(YellowIfColored(warning));
            }

            return sb.ToString();
        }

        private string GetProjectReportErrorContent(ProjectMigrationReport projectMigrationReport, bool colored)
        {
            StringBuilder sb = new StringBuilder();
            Func<string, string> RedIfColored = (str) => colored ? str.Red() : str;

            if (projectMigrationReport.Errors.Any())
            {
                sb.AppendLine(RedIfColored(string.Format(
                    LocalizableStrings.ProjectMigrationFailed,
                    projectMigrationReport.ProjectName,
                    projectMigrationReport.ProjectDirectory)));

                foreach (var error in projectMigrationReport.Errors.Select(e => e.GetFormattedErrorMessage()))
                {
                    sb.AppendLine(RedIfColored(error));
                }
            }

            return sb.ToString();
        }

        private TextWriter GetReportFileOutputTextWriter()
        {
            return File.CreateText(_reportFile);
        }

        private IEnumerable<string> GetProjectsToMigrate(string projectArg)
        {
            IEnumerable<string> projects = null;

            if (projectArg.EndsWith(Project.FileName, StringComparison.OrdinalIgnoreCase))
            {
                projects = Enumerable.Repeat(projectArg, 1);
            }
            else if (projectArg.EndsWith(GlobalSettings.FileName, StringComparison.OrdinalIgnoreCase))
            {
                projects = GetProjectsFromGlobalJson(projectArg);

                if (!projects.Any())
                {
                    throw new GracefulException(LocalizableStrings.MigrationFailedToFindProjectInGlobalJson);
                }
            }
            else if (File.Exists(projectArg) &&
                     string.Equals(Path.GetExtension(projectArg), ".sln", StringComparison.OrdinalIgnoreCase))
            {
                projects = GetProjectsFromSolution(projectArg);

                if (!projects.Any())
                {
                    throw new GracefulException(
                        string.Format(LocalizableStrings.MigrationUnableToFindProjects, projectArg));
                }
            }
            else if (Directory.Exists(projectArg))
            {
                projects = Directory.EnumerateFiles(projectArg, Project.FileName, SearchOption.AllDirectories);

                if (!projects.Any())
                {
                    throw new GracefulException(
                        string.Format(LocalizableStrings.MigrationProjectJsonNotFound, projectArg));
                }
            }
            else
            {
                throw new GracefulException(
                    string.Format(LocalizableStrings.MigrationInvalidProjectArgument, projectArg));
            }

            foreach (var project in projects)
            {
                yield return GetProjectJsonPath(project);
            }
        }

        private void EnsureNotNull(string variable, string message)
        {
            if (variable == null)
            {
                throw new GracefulException(message);
            }
        }

        private string GetProjectJsonPath(string projectJson)
        {
            projectJson = ProjectPathHelper.NormalizeProjectFilePath(projectJson);

            if (File.Exists(projectJson))
            {
                return projectJson;
            }

            throw new GracefulException(string.Format(LocalizableStrings.MigratonUnableToFindProjectJson, projectJson));
        }

        private IEnumerable<string> GetProjectsFromGlobalJson(string globalJson)
        {
            var searchPaths = ProjectDependencyFinder.GetGlobalPaths(GetGlobalJsonDirectory(globalJson));

            foreach (var searchPath in searchPaths)
            {
                var directory = new DirectoryInfo(searchPath);

                if (!directory.Exists)
                {
                    continue;
                }

                foreach (var projectDirectory in directory.EnumerateDirectories())
                {
                    var projectFilePath = Path.Combine(projectDirectory.FullName, Project.FileName);

                    if (File.Exists(projectFilePath))
                    {
                        yield return projectFilePath;
                    }
                }
            }
        }

        private string GetGlobalJsonDirectory(string globalJson)
        {
            if (!File.Exists(globalJson))
            {
                throw new GracefulException(
                    string.Format(LocalizableStrings.MigrationUnableToFindGlobalJson, globalJson));
            }

            var globalJsonDirectory = Path.GetDirectoryName(globalJson);
            return string.IsNullOrEmpty(globalJsonDirectory) ? "." : globalJsonDirectory;
        }

        private IEnumerable<string> GetProjectsFromSolution(string slnPath)
        {
            if (!File.Exists(slnPath))
            {
                throw new GracefulException(
                    string.Format(LocalizableStrings.MigrationUnableToFindSolutionFile, slnPath));
            }

            _slnFile = SlnFile.Read(slnPath);

            foreach (var project in _slnFile.Projects)
            {
                var projectFilePath = Path.Combine(
                    _slnFile.BaseDirectory,
                    Path.GetDirectoryName(project.FilePath),
                    Project.FileName);

                if (File.Exists(projectFilePath))
                {
                    yield return projectFilePath;
                }
            }
        }
    }
}