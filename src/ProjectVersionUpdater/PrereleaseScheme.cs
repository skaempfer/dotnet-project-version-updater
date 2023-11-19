using System.Collections.Generic;
using System.Linq;

using NuGet.Versioning;

namespace ProjectVersionUpdater;

public interface IPrereleaseScheme
{
    SemanticVersion Next(SemanticVersion version, VersionPart part);
}

public class CustomPrereleaseScheme : IPrereleaseScheme
{
    private readonly string name;
    private readonly ReleaseLabelParser labelParser;

    public CustomPrereleaseScheme(string name)
    {
        this.name = name;
        this.labelParser = new ReleaseLabelParser(name);
    }

    public SemanticVersion Next(SemanticVersion version, VersionPart part)
    {
        return !version.IsPrerelease || !this.labelParser.TryParseRelease(version, out (string name, int increment) labels)
            ? version.Increase(part).SetReleaseLabel($"{this.name}.1")
            : new SemanticVersion(
                major: version.Major,
                minor: version.Minor,
                patch: version.Patch,
                releaseLabel: $"{this.name}.{++labels.increment}");
    }
}
