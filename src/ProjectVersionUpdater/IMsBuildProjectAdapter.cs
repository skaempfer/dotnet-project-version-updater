using System.Collections.Generic;

namespace ProjectVersionUpdater;

/// <summary>
/// Creates a relationship between a build-centric project representation and a code analysis-centric project representation 
/// </summary>
/// <remarks>
/// A <see cref="Microsoft.Build.Evaluation.Project"/> is used for reading and writing version information, while a <see cref="Microsoft.CodeAnalysis.Project"/>
/// is used for accessing the project dependency graph.
/// Multiple <see cref="Microsoft.CodeAnalysis.Project"/>s can point to the same <see cref="Microsoft.Build.Evaluation.Project"/>, i.e. when having a
/// multi-targeted project. Such a project appears e.g. as ProjectA(net5) and ProjectA(net6).
/// </remarks>
public interface IMsbuildProjectAdapter
{
    Microsoft.Build.Evaluation.Project LoadBuildProject(Microsoft.CodeAnalysis.Project solutionProject);

    IReadOnlyCollection<Microsoft.Build.Evaluation.Project> LoadBuildProjects(IEnumerable<Microsoft.CodeAnalysis.Project> solutionProjects);

    void SaveProject(Microsoft.Build.Evaluation.Project msbuildProject);
}
