using System.Collections.Generic;
using System.Linq;

using NuGet.Versioning;

namespace ProjectVersionUpdater;

public class ProjectVersionUpdaterFactory
{
    public Microsoft.CodeAnalysis.Solution Solution { get; }

    public IPrereleaseScheme PrereleaseScheme { get; }

    public IMsbuildProjectAdapter ProjectAdapter { get; }

    public ProjectVersionUpdaterFactory(Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme)
        : this(solution, prereleaseScheme, new FileSystemProjectAdapter())
    {
    }

    public ProjectVersionUpdaterFactory(Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
    {
        this.Solution = solution;
        this.PrereleaseScheme = prereleaseScheme;
        this.ProjectAdapter = projectAdapter;
    }

    public ProjectVersionUpdater Create(string projectToUpdate)
        => this.Create(new string[] { projectToUpdate });

    public ProjectVersionUpdater Create(IEnumerable<string> projectsToUpdate)
        => new ProjectVersionUpdater(projectsToUpdate, this.Solution, this.PrereleaseScheme, this.ProjectAdapter);
}

public class ProjectVersionUpdater
{
    public Microsoft.CodeAnalysis.Solution Solution { get; }

    public IPrereleaseScheme PrereleaseScheme { get; }

    public IMsbuildProjectAdapter ProjectAdapter { get; }

    public IEnumerable<Microsoft.CodeAnalysis.Project> ProjectsToUpdate { get; }

    public ProjectVersionUpdater(string projectToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
        : this(new string[] { projectToUpdate }, solution, prereleaseScheme, projectAdapter)
    {
    }

    public ProjectVersionUpdater(IEnumerable<string> projectsToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
    {
        this.Solution = solution;
        this.PrereleaseScheme = prereleaseScheme;
        this.ProjectAdapter = projectAdapter;

        List<Microsoft.CodeAnalysis.Project> projects = new();
        foreach (string projectPath in projectsToUpdate)
        {
            projects.AddRange(this.Solution.Projects.Where(p => p.FilePath.Equals(projectPath)));
        }

        this.ProjectsToUpdate = projects;
    }

    public ProjectVersionUpdater(Microsoft.CodeAnalysis.Project projectToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
    : this(new Microsoft.CodeAnalysis.Project[] { projectToUpdate }, solution, prereleaseScheme, projectAdapter)
    {
    }

    public ProjectVersionUpdater(IEnumerable<Microsoft.CodeAnalysis.Project> projectsToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
    {
        this.Solution = solution;
        this.PrereleaseScheme = prereleaseScheme;
        this.ProjectAdapter = projectAdapter;
        this.ProjectsToUpdate = projectsToUpdate;
    }

    public void IncreaseVersion(VersionPart part, bool prerelease)
    {
        IReadOnlyCollection<Microsoft.Build.Evaluation.Project> msBuildProjects = this.ProjectAdapter.LoadBuildProjects(this.ProjectsToUpdate);
        foreach (Microsoft.Build.Evaluation.Project msBuildProject in msBuildProjects)
        {
            this.IncreaseVersion(msBuildProject, part, prerelease);
        }
    }

    private void IncreaseVersion(Microsoft.Build.Evaluation.Project project, VersionPart part, bool prerelease)
    {
        SemanticVersion currentVersion = project.GetVersion();

        // TODO: Check if version prop is set: Either throw exception or create prop

        if (prerelease)
        {
            SemanticVersion newVersion = this.PrereleaseScheme.Next(currentVersion, part);
            project.SetVersion(newVersion);
        }
        else
        {
            SemanticVersion newVersion = currentVersion.Increase(part);
            project.SetVersion(newVersion);
        }

        this.ProjectAdapter.SaveProject(project);
    }

    public void IncreaseDependantsVersion()
    {
        foreach (Microsoft.CodeAnalysis.Project dependant in this.GetDependants())
        {
            Microsoft.Build.Evaluation.Project msbuildDependant = this.ProjectAdapter.LoadBuildProject(dependant);
            SemanticVersion dependantVersion = msbuildDependant.GetVersion();

            if (dependantVersion == null)
            {
                // This dependant is not versioned and can be skipped, e.g. test projects
                continue;
            }

            if (this.IsPrerelease())
            {
                SemanticVersion newDependantVersion = this.PrereleaseScheme.Next(dependantVersion, VersionPart.Patch);
                msbuildDependant.SetVersion(newDependantVersion);
            }
            else
            {
                msbuildDependant.SetVersion(dependantVersion.IncreasePatch());
            }

            this.ProjectAdapter.SaveProject(msbuildDependant);
        }
    }

    private IEnumerable<Microsoft.CodeAnalysis.Project> GetDependants()
    {
        Microsoft.CodeAnalysis.ProjectDependencyGraph dependencyGraph = this.Solution.GetProjectDependencyGraph();
        HashSet<Microsoft.CodeAnalysis.ProjectId> dependantIds = new();
        foreach (Microsoft.CodeAnalysis.Project project in this.ProjectsToUpdate)
        {
            IEnumerable<Microsoft.CodeAnalysis.ProjectId> projectDependants = dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(project.Id);
            dependantIds = dependantIds.Concat(new HashSet<Microsoft.CodeAnalysis.ProjectId>(projectDependants)).ToHashSet();
        }

        IEnumerable<Microsoft.CodeAnalysis.Project> dependants = dependantIds.Select(id => this.Solution.GetProject(id));
        dependants = dependants.Except(this.ProjectsToUpdate);

        return dependants;
    }

    private bool IsPrerelease()
    {
        List<Microsoft.Build.Evaluation.Project> msbuildProjects = new();
        foreach (Microsoft.CodeAnalysis.Project solutionProject in this.ProjectsToUpdate)
        {
            msbuildProjects.Add(this.ProjectAdapter.LoadBuildProject(solutionProject));
        }

        return msbuildProjects.Any(p => p.GetVersion().IsPrerelease);
    }
}
