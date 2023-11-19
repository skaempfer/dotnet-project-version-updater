using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;

using Microsoft.Build.Construction;
using Microsoft.Build.Locator;

namespace ProjectVersionUpdater;

public static class Program
{
    static Program()
    {
        VisualStudioInstance net6Instance = MSBuildLocator.QueryVisualStudioInstances().First(i => i.Version.Major == 6);
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterInstance(net6Instance);
        }
    }

    public static async Task Main(string[] args)
    {
        Parser parser = new Parser(config =>
        {
            config.CaseInsensitiveEnumValues = true;
            config.AutoHelp = true;
            config.HelpWriter = Parser.Default.Settings.HelpWriter;
        });
        ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);

        await parserResult.WithParsedAsync(async (options) =>
        {
            // TODO: Validate project path
            options.ProjectPaths = options.ProjectPaths.Select(p => Path.GetFullPath(p));

            using Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();

            // TODO: We currently do not validate the assumption that both projects are in the same solution
            Microsoft.CodeAnalysis.Solution solution = await workspace.OpenSolutionAsync(GetContainingSolutionPath(options.ProjectPaths.First()));

            IPrereleaseScheme prereleaseScheme = new CustomPrereleaseScheme(options.ReleaseName);

            ProjectVersionUpdaterFactory updaterFactory = new ProjectVersionUpdaterFactory(solution, prereleaseScheme);

            ProjectVersionUpdater updater = updaterFactory.Create(options.ProjectPaths);

            updater.IncreaseVersion(options.UpdatePart, options.Prerelease);

            if (options.UpdateDependants)
            {
                updater.IncreaseDependantsVersion();
            }
        });
    }

    private static string GetContainingSolutionPath(string projectPath)
    {
        string projectDirectory = Path.GetDirectoryName(projectPath);
        return WalkUpDirectory(projectDirectory, 0);

        string WalkUpDirectory(string directory, int levelsWalked)
        {
            if (levelsWalked > 3)
            {
                throw new ArgumentException(
                    $"Cannot find a solution file for project '{projectDirectory}' in the project directory or 3 parent directories.");
            }

            // TODO: Handle case of more than one sln file per directory
            string solutionPath = Directory.GetFiles(directory, "*.sln").SingleOrDefault();

            return solutionPath ?? WalkUpDirectory(Directory.GetParent(directory).ToString(), ++levelsWalked);
        }
    }
}

public interface IMsbuildProjectAdapter
{
    Microsoft.Build.Evaluation.Project LoadProject(Microsoft.CodeAnalysis.Project solutionProject);

    void SaveProject(Microsoft.Build.Evaluation.Project msbuildProject);
}

public class FileSystemProjectAdapter : IMsbuildProjectAdapter
{
    private readonly Microsoft.Build.Evaluation.ProjectCollection collection = new();

    private readonly Dictionary<Microsoft.CodeAnalysis.Project, Microsoft.Build.Evaluation.Project> lookup = new();

    public Microsoft.Build.Evaluation.Project LoadProject(Microsoft.CodeAnalysis.Project solutionProject)
    {
        if (!this.lookup.TryGetValue(solutionProject, out Microsoft.Build.Evaluation.Project buildProject))
        {
            buildProject = new Microsoft.Build.Evaluation.Project(ProjectRootElement.Open(solutionProject.FilePath, this.collection, preserveFormatting: true));
            this.lookup.Add(solutionProject, buildProject);
        }

        return buildProject;
    }

    public void SaveProject(Microsoft.Build.Evaluation.Project msbuildProject)
        => msbuildProject.Save();
}
