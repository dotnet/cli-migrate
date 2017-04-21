namespace Microsoft.DotNet.Tools.MigrateCommand
{
    public interface ICanManipulateSolutionFile
    {
        void AddProjectToSolution(string solutionFilePath, string projectFilePath);

        void RemoveProjectFromSolution(string solutionFilePath, string projectFilePath);
    }
}
