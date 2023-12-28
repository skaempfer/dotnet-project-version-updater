using NuGet.Versioning;

namespace ProjectVersionUpdater;

public class NamedPrereleaseScheme : IPrereleaseScheme
{
    private readonly string name;
    private readonly ReleaseLabelParser labelParser;

    public NamedPrereleaseScheme(string name)
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
