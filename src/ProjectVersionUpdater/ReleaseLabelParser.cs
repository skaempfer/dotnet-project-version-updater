using System.Collections.Generic;
using System.Linq;

using NuGet.Versioning;

namespace ProjectVersionUpdater;

public class ReleaseLabelParser
{
    private readonly IEnumerable<string> labelNames;

    public ReleaseLabelParser(string labelName)
        : this(new string[] { labelName })
    {
    }

    public ReleaseLabelParser(IEnumerable<string> labelNames)
    {
        this.labelNames = labelNames;
    }

    public bool TryParseRelease(SemanticVersion version, out (string name, int increment) prerelease)
    {
        if (!version.IsPrerelease)
        {
            prerelease = (null, 0);
            return false;
        }

        string[] parts = @version.Release.Split('.');

        if (parts.Length != 2)
        {
            prerelease = (null, 0);
            return false;
        }

        if (!int.TryParse(parts[1], out int increment))
        {
            prerelease = (null, 0);
            return false;
        }

        string name = parts[0];
        if (!this.labelNames.Contains(name))
        {
            prerelease = (null, 0);
            return false;
        }

        prerelease = (name, increment);
        return true;
    }
}
