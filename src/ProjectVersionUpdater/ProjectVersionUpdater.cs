using System.Collections.Generic;
using System.Linq;

using NuGet.Versioning;

using RoslynProject = Microsoft.CodeAnalysis.Project;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

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

    public IEnumerable<RoslynProject> ProjectsToUpdate { get; }

    public ProjectVersionUpdater(string projectToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
        : this(new string[] { projectToUpdate }, solution, prereleaseScheme, projectAdapter)
    {
    }

    public ProjectVersionUpdater(IEnumerable<string> projectsToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
    {
        this.Solution = solution;
        this.PrereleaseScheme = prereleaseScheme;
        this.ProjectAdapter = projectAdapter;

        List<RoslynProject> projects = new();
        foreach (string projectPath in projectsToUpdate)
        {
            projects.AddRange(this.Solution.Projects.Where(p => p.FilePath.Equals(projectPath)));
        }

        this.ProjectsToUpdate = projects;
    }

    public ProjectVersionUpdater(RoslynProject projectToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
    : this(new RoslynProject[] { projectToUpdate }, solution, prereleaseScheme, projectAdapter)
    {
    }

    public ProjectVersionUpdater(IEnumerable<RoslynProject> projectsToUpdate, Microsoft.CodeAnalysis.Solution solution, IPrereleaseScheme prereleaseScheme, IMsbuildProjectAdapter projectAdapter)
    {
        this.Solution = solution;
        this.PrereleaseScheme = prereleaseScheme;
        this.ProjectAdapter = projectAdapter;
        this.ProjectsToUpdate = projectsToUpdate;
    }

    public void IncreaseVersion(VersionPart part, bool prerelease)
    {
        IReadOnlyCollection<MsBuildProject> msBuildProjects = this.ProjectAdapter.LoadBuildProjects(this.ProjectsToUpdate);
        foreach (MsBuildProject msBuildProject in msBuildProjects)
        {
            this.IncreaseVersion(msBuildProject, part, prerelease);
        }
    }

    private void IncreaseVersion(MsBuildProject project, VersionPart part, bool prerelease)
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
        IReadOnlyCollection<MsBuildProject> dependants = this.ProjectAdapter.LoadBuildProjects(this.GetDependants());
        foreach (MsBuildProject dependant in dependants)
        {
            SemanticVersion dependantVersion = dependant.GetVersion();

            if (dependantVersion == null)
            {
                // This dependant is not versioned and can be skipped, e.g. test projects
                continue;
            }

            if (this.IsPrerelease())
            {
                SemanticVersion newDependantVersion = this.PrereleaseScheme.Next(dependantVersion, VersionPart.Patch);
                dependant.SetVersion(newDependantVersion);
            }
            else
            {
                dependant.SetVersion(dependantVersion.IncreasePatch());
            }

            this.ProjectAdapter.SaveProject(dependant);
        }
    }

    private IEnumerable<RoslynProject> GetDependants()
    {
        Microsoft.CodeAnalysis.ProjectDependencyGraph dependencyGraph = this.Solution.GetProjectDependencyGraph();
        HashSet<Microsoft.CodeAnalysis.ProjectId> dependantIds = new();
        foreach (RoslynProject project in this.ProjectsToUpdate)
        {
            IEnumerable<Microsoft.CodeAnalysis.ProjectId> projectDependants = dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(project.Id);
            dependantIds = dependantIds.Concat(new HashSet<Microsoft.CodeAnalysis.ProjectId>(projectDependants)).ToHashSet();
        }

        IEnumerable<RoslynProject> dependants = dependantIds.Select(id => this.Solution.GetProject(id));
        dependants = dependants.Except(this.ProjectsToUpdate);

        return dependants;
    }

    private bool IsPrerelease()
    {
        List<MsBuildProject> msbuildProjects = new();
        foreach (RoslynProject solutionProject in this.ProjectsToUpdate)
        {
            msbuildProjects.Add(this.ProjectAdapter.LoadBuildProject(solutionProject));
        }

        return msbuildProjects.Any(p => p.GetVersion().IsPrerelease);
    }
}
