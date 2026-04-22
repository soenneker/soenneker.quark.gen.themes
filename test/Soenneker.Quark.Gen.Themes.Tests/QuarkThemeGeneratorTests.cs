using Soenneker.Tests.HostedUnit;

namespace Soenneker.Quark.Gen.Themes.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class QuarkThemeGeneratorTests : HostedUnitTest
{
    public QuarkThemeGeneratorTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
