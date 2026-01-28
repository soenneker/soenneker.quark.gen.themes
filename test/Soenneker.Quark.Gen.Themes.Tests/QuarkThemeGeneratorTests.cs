using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Quark.Gen.Themes.Tests;

[Collection("Collection")]
public sealed class QuarkThemeGeneratorTests : FixturedUnitTest
{
    public QuarkThemeGeneratorTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {

    }
}
