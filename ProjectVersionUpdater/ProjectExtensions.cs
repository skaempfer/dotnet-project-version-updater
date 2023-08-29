using System;

using Microsoft.Build.Evaluation;

using NuGet.Versioning;

namespace ProjectVersionUpdater;

public static class ProjectExtensions
{
    public static SemanticVersion GetVersion(this Project project)
    {
        ProjectProperty versionProperty = project.GetProperty("Version");
        return versionProperty.IsImported
            ? null
            : SemanticVersion.Parse(versionProperty.EvaluatedValue);
    }

    public static void SetVersion(this Project project, string version)
    {
        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        if (!SemanticVersion.TryParse(version, out SemanticVersion semanticVersion))
        {
            throw new ArgumentException($"Argument '{version}' must follow semantic versioning syntax.");
        }

        project.SetVersion(semanticVersion);
    }

    public static void SetVersion(this Project project, SemanticVersion version)
    {
        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        project.SetProperty("Version", version.ToString());
    }
}
