using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Soenneker.Quark.Gen.Themes.Tests;

public sealed class ThemeIncrementalTests
{
    private const string Source = """
namespace Soenneker.Quark { public class Theme { } }
[Soenneker.Quark.Gen.Themes.GenerateQuarkThemeCss("css/theme.css", BuildMinified = false)]
public partial class DemoTheme { public static Soenneker.Quark.Theme Create() => new(); }
""";

    [Test]
    public void Unrelated_edits_reuse_the_manifest_and_relevant_edits_update_it()
    {
        CSharpCompilation compilation = Create(Source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuarkThemeCssGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        string before = Manifest(driver);
        if (!before.Contains("DemoTheme|css/theme.css|1|0|tailwind/quark-theme.generated.css|1", StringComparison.Ordinal))
            throw new Exception("Theme options were not preserved.");

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Unrelated { public int Value => 42; }")));
        if (Manifest(driver) != before)
            throw new Exception("An unrelated edit changed the manifest.");
        var steps = driver.GetRunResult().Results[0].TrackedSteps["ThemeCandidates"];
        if (steps.SelectMany(step => step.Outputs).Any(output => output.Reason is not (IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)))
            throw new Exception("Unrelated edits invalidated the theme pipeline.");

        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(Source.Replace("css/theme.css", "css/changed.css")));
        driver = driver.RunGenerators(compilation);
        if (!Manifest(driver).Contains("css/changed.css", StringComparison.Ordinal))
            throw new Exception("A relevant edit was not propagated.");
    }

    [Test]
    public void Diagnostics_follow_changed_factory_and_missing_type()
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new QuarkThemeCssGenerator());
        driver = driver.RunGenerators(Create(Source.Replace("Create()", "Create(int unsupported)")));
        if (!driver.GetRunResult().Diagnostics.Any(d => d.Id == "QTG002"))
            throw new Exception("Missing factory diagnostic was lost.");
        driver = driver.RunGenerators(Create(Source.Replace("css/theme.css", " ")));
        if (!driver.GetRunResult().Diagnostics.Any(d => d.Id == "QTG001"))
            throw new Exception("Missing path diagnostic was lost.");
        driver = driver.RunGenerators(Create(Source.Replace("namespace Soenneker.Quark { public class Theme { } }", "")));
        if (driver.GetRunResult().Diagnostics.Count(d => d.Id == "QTG004") != 1)
            throw new Exception("Missing type diagnostic changed.");
    }

    [Test]
    public void Factory_changes_in_another_partial_declaration_invalidate_diagnostics()
    {
        SyntaxTree factory = CSharpSyntaxTree.ParseText("public partial class DemoTheme { public static Soenneker.Quark.Theme Create() => new(); }");
        CSharpCompilation compilation = Create(Source.Replace("public static Soenneker.Quark.Theme Create() => new();", "")).AddSyntaxTrees(factory);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new QuarkThemeCssGenerator());
        driver = driver.RunGenerators(compilation);
        if (driver.GetRunResult().Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            throw new Exception("Valid partial theme failed.");

        compilation = compilation.ReplaceSyntaxTree(factory, CSharpSyntaxTree.ParseText("public partial class DemoTheme { }"));
        driver = driver.RunGenerators(compilation);
        if (!driver.GetRunResult().Diagnostics.Any(d => d.Id == "QTG002"))
            throw new Exception("A changed partial factory did not invalidate the cached candidate.");
    }

    private static CSharpCompilation Create(string source) => CSharpCompilation.Create("Themes", [CSharpSyntaxTree.ParseText(source)],
        [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static string Manifest(GeneratorDriver driver) => driver.GetRunResult().Results[0].GeneratedSources
        .Single(source => source.HintName == "QuarkThemeCssManifest.g.cs").SourceText.ToString();
}
