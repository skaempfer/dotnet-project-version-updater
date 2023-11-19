using NuGet.Versioning;

using Xunit;

namespace ProjectVersionUpdater.Tests;

public class ReleaseLabelParserTests
{
    [Theory]
    [InlineData("1.0.0-xyz.1", "xyz", 1)]
    public void TryParseRelease_CanParse(string version, string expectedName, int expectedIncrement)
    {
        // Arrange
        ReleaseLabelParser sut = new ReleaseLabelParser("xyz");

        // Act
        bool result = sut.TryParseRelease(SemanticVersion.Parse(version), out (string name, int increment) actual);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedName, actual.name);
        Assert.Equal(expectedIncrement, actual.increment);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.0.0-major.1")]
    [InlineData("1.0.0-feature")]
    [InlineData("1.0.0-feature.one")]
    [InlineData("1.0.0-one.two.three")]
    public void TryParseRelease_DefaultScheme_CannotParse(string version)
    {
        // Arrange
        ReleaseLabelParser sut = new ReleaseLabelParser("foo");

        // Act
        bool result = sut.TryParseRelease(SemanticVersion.Parse(version), out (string name, int increment) actual);

        // Assert
        Assert.False(result);
        Assert.Null(actual.name);
        Assert.Equal(0, actual.increment);
    }
}
