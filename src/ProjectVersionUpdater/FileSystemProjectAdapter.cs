using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.Build.Construction;
using Microsoft.Build.Framework;

using MsBuildProject = Microsoft.Build.Evaluation.Project;
using RoslynProject = Microsoft.CodeAnalysis.Project;

namespace ProjectVersionUpdater;

/// <summary>
/// An implementation of <see cref="IMsbuildProjectAdapter"/> that uses the file system for reading and writing project information.
/// </summary>
public class FileSystemProjectAdapter : IMsbuildProjectAdapter
{
    private readonly Microsoft.Build.Evaluation.ProjectCollection collection = new();

    private readonly Dictionary<RoslynProject, MsBuildProject> loadedMsBuildProjects = new();

    private readonly RoslynProjectEqualityComparer roslynProjectEqualityComparer = new();

    public MsBuildProject LoadBuildProject(RoslynProject solutionProject)
    {
        this.LoadMsBuildProject(solutionProject);

        return this.loadedMsBuildProjects[solutionProject];
    }

    public IReadOnlyCollection<MsBuildProject> LoadBuildProjects(IEnumerable<RoslynProject> solutionProjects)
    {
        foreach(RoslynProject roslynProject in solutionProjects)
        {
            this.LoadMsBuildProject(roslynProject);
        }

        List<MsBuildProject> msBuildProjects = new();
        foreach(RoslynProject project in solutionProjects)
        {
            msBuildProjects.Add(this.loadedMsBuildProjects[project]);
        }

        msBuildProjects = msBuildProjects.Distinct().ToList();

        return msBuildProjects.AsReadOnly();
    }

    private void LoadMsBuildProject(RoslynProject roslynProject)
    {
        if (this.loadedMsBuildProjects.Keys.Contains(roslynProject, this.roslynProjectEqualityComparer))
        {
            return;
        }

        MsBuildProject loadedMsBuildProject = this.loadedMsBuildProjects.Values.SingleOrDefault(x => x.FullPath.Equals(roslynProject.FilePath));
        loadedMsBuildProject??= new MsBuildProject(ProjectRootElement.Open(roslynProject.FilePath, this.collection, preserveFormatting: true));

        this.loadedMsBuildProjects.Add(roslynProject, loadedMsBuildProject);
    }

    public void SaveProject(MsBuildProject msbuildProject)
        => msbuildProject.Save();

    private class RoslynProjectEqualityComparer : IEqualityComparer<RoslynProject>
    {
        public bool Equals(RoslynProject x, RoslynProject y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (ReferenceEquals(x, null))
            {
                return false;
            }
            if (ReferenceEquals(y, null))
            {
                return false;
            }
            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Name.Equals(y.Name);
        }

        public int GetHashCode([DisallowNull] RoslynProject obj) => obj.GetHashCode();
    }
}
