using System;

using NuGet.Versioning;

namespace ProjectVersionUpdater;

public static class SemanticVersionExtensions
{
    public static SemanticVersion IncreaseMajor(this SemanticVersion @this) => new SemanticVersion(
            major: @this.IsPrerelease ? @this.Major : @this.Major + 1,
            minor: 0,
            patch: 0);

    public static SemanticVersion IncreaseMinor(this SemanticVersion @this) => new SemanticVersion(
            major: @this.Major,
            minor: @this.IsPrerelease ? @this.Minor : @this.Minor + 1,
            patch: 0);

    public static SemanticVersion IncreasePatch(this SemanticVersion @this) => new SemanticVersion(
            major: @this.Major,
            minor: @this.Minor,
            patch: @this.IsPrerelease ? @this.Patch : @this.Patch + 1);

    public static SemanticVersion Increase(this SemanticVersion @this, VersionPart part) =>
        part switch
        {
            VersionPart.Major => @this.IncreaseMajor(),
            VersionPart.Minor => @this.IncreaseMinor(),
            VersionPart.Patch => @this.IncreasePatch(),
            _ => throw new ArgumentException($"Unknown value for parameter {nameof(part)}: {part}.")
        };

    public static SemanticVersion SetReleaseLabel(this SemanticVersion @this, string label) => new SemanticVersion(
        major: @this.Major,
        minor: @this.Minor,
        patch: @this.Patch,
        releaseLabel: label);

    public static SemanticVersion RemoveReleaseLabel(this SemanticVersion @this) => new SemanticVersion(
            major: @this.Major,
            minor: @this.Minor,
            patch: @this.Patch);
}
