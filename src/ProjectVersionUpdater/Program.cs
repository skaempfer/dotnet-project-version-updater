using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;

using Microsoft.Build.Locator;

namespace ProjectVersionUpdater;

public static class Program
{
    static Program()
    {
        VisualStudioInstance net6Instance = MSBuildLocator.QueryVisualStudioInstances().First(i => i.Version.Major == 8);
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
            options.ProjectPaths = options.ProjectPaths.Select(p => Path.GetFullPath(p));
            ValidateProjectPaths(options.ProjectPaths);

            using Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();

            options.SolutionPath ??= GetContainingSolutionPath(options.ProjectPaths.First());
            Microsoft.CodeAnalysis.Solution solution = await workspace.OpenSolutionAsync(options.SolutionPath);

            options.PrereleaseName ??= "pre";
            IPrereleaseScheme prereleaseScheme = new NamedPrereleaseScheme(options.PrereleaseName);

            ProjectVersionUpdaterFactory updaterFactory = new ProjectVersionUpdaterFactory(solution, prereleaseScheme);

            ProjectVersionUpdater updater = updaterFactory.Create(options.ProjectPaths);

            options.UpdatePart ??= VersionPart.Patch;
            updater.IncreaseVersion(options.UpdatePart.Value, options.IsPrerelease);

            if (options.UpdateDependants)
            {
                updater.IncreaseDependantsVersion();
            }
        });
    }

    private static void ValidateProjectPaths(IEnumerable<string> projectPaths)
    {
        foreach (string path in projectPaths)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File '{path}' does not exist.");
            }
        }
    }

    private static string GetContainingSolutionPath(string projectPath)
    {
        string projectDirectory = Path.GetDirectoryName(projectPath);
        return WalkUpDirectory(projectDirectory, 0);

        string WalkUpDirectory(string directory, int levelsWalked)
        {
            if (levelsWalked > 5)
            {
                throw new ArgumentException(
                    $"Cannot find a solution file for project '{projectDirectory}' in the project directory or {5} parent directories.");
            }

            string[] candidatePaths = Directory.GetFiles(directory, "*.sln");

            if(candidatePaths.Length > 1)
            {
                throw new InvalidOperationException($"There is more than one solution file at '{directory}'. Cannot automatically decide which one to use. Specify a solution file manually.");
            }

            string solutionPath = candidatePaths.SingleOrDefault();

            return solutionPath ?? WalkUpDirectory(Directory.GetParent(directory).ToString(), ++levelsWalked);
        }
    }
}
