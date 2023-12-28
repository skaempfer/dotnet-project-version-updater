using System.Collections.Generic;

using CommandLine;
using CommandLine.Text;

namespace ProjectVersionUpdater;

public class Options
{
    [Value(0, MetaName = "ProjectPaths", Required = true, HelpText = "List of paths to the project files to update.")]
    public IEnumerable<string> ProjectPaths { get; set; }

    [Option('s', "solution", Required = false, HelpText = "Path to the solution file the project(s) to update is/are part of. If omitted the next solution file relative to the first provided project path is used.")]
    public string SolutionPath { get; set; }

    [Option('u', "update", Required = false, HelpText = "Indicates which version part to increase: major, minor or patch. Defaults to major if omitted.")]
    public VersionPart UpdatePart { get; set; }

    [Option('p', "prerelease", Required = false, HelpText = "Indicates if version update should be a prerelease.")]
    public bool Prerelease { get; set; }

    [Option('n', "name", Required = false, HelpText = "Use a custom name for prerelease label. If omitted the default naming scheme 'pre' is used.")]
    public string ReleaseName { get; set; }

    [Option('d', "dependants", Required = false, HelpText = "Indicates if all projects which are (transitevely) dependent on the project to update should be updated as well.")]
    public bool UpdateDependants { get; set; }

    [Usage(ApplicationAlias = "dotnet update-project")]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example(
                "Update a project to its next major version.Update all its dependent projects to the next patch version",
                new Options
                {
                    UpdatePart = VersionPart.Major,
                    UpdateDependants = true,
                    ProjectPaths = new string[] { ".\\Project.csproj" }
                });
            yield return new Example(
                "Update a project to its next minor prerelease version. Update all its dependent projects to the next patch prerelease version",
                new Options
                {
                    UpdatePart = VersionPart.Minor,
                    UpdateDependants = true,
                    Prerelease = true,
                    ProjectPaths = new string[] { ".\\Project.csproj" }
                });
            yield return new Example(
                "Update multiple projects to its next release version. Update all their dependant projects to the next patch release",
                new Options
                {
                    UpdatePart = VersionPart.Major,
                    UpdateDependants = true,
                    ProjectPaths = new string[] { ".\\Project.csproj", ".\\Project.Abstractions.csproj" }
                });
        }
    }
}
