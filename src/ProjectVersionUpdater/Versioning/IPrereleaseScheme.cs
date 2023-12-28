using NuGet.Versioning;

namespace ProjectVersionUpdater;

public interface IPrereleaseScheme
{
    SemanticVersion Next(SemanticVersion version, VersionPart part);
}
