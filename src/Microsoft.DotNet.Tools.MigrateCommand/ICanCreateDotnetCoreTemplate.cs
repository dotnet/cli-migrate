namespace Microsoft.DotNet.Tools.MigrateCommand
{
    public interface ICanCreateDotnetCoreTemplate
    {
        void CreateWithWithEphemeralHiveAndNoRestore(string templateName, string outputDirectory, string workingDirectory);
    }
}
