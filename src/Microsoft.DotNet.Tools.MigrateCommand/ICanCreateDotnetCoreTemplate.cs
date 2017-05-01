namespace Microsoft.DotNet.Tools.MigrateCommand
{
    public interface ICanCreateDotnetCoreTemplate
    {
        void CreateWithEphemeralHiveAndNoRestore(string templateName, string outputDirectory, string workingDirectory);
    }
}
