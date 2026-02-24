using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Soenneker.Quark.Gen.Themes
{
    [Generator]
    public sealed class QuarkThemeCssGenerator : IIncrementalGenerator
    {
        private const string _suiteAttributeName = "Soenneker.Quark.GenerateQuarkThemeCssAttribute";
        private const string _generatorAttributeName = "Soenneker.Quark.Gen.Themes.GenerateQuarkThemeCssAttribute";

        private static readonly DiagnosticDescriptor _missingOutputPath = new(
            "QTG001",
            "Missing output file path",
            "GenerateQuarkThemeCssAttribute on '{0}' requires a non-empty output file path",
            "QuarkThemeGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor _themeFactoryMissing = new(
            "QTG002",
            "Theme factory missing",
            "GenerateQuarkThemeCssAttribute on '{0}' requires a single public static method or property returning Soenneker.Quark.Theme (optionally taking IServiceProvider)",
            "QuarkThemeGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor _themeTypeMissing = new(
            "QTG004",
            "Theme type missing",
            "Unable to locate Soenneker.Quark.Theme in referenced assemblies",
            "QuarkThemeGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<Candidate> suiteThemeClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
                _suiteAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => new Candidate((INamedTypeSymbol)ctx.TargetSymbol, ctx.Attributes.First()));

            IncrementalValuesProvider<Candidate> generatorThemeClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
                _generatorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => new Candidate((INamedTypeSymbol)ctx.TargetSymbol, ctx.Attributes.First()));

            IncrementalValueProvider<((Compilation Left, ImmutableArray<Candidate> Right) Left, ImmutableArray<Candidate> Right)> combined = context.CompilationProvider
                                                                                                                                                    .Combine(suiteThemeClasses.Collect())
                                                                                                                                                    .Combine(generatorThemeClasses.Collect());

            context.RegisterSourceOutput(combined, static (spc, data) =>
            {
                Compilation? compilation = data.Left.Left;
                ImmutableArray<Candidate> suiteCandidates = data.Left.Right;
                ImmutableArray<Candidate> generatorCandidates = data.Right;

                if (suiteCandidates.IsDefaultOrEmpty && generatorCandidates.IsDefaultOrEmpty)
                    return;

                INamedTypeSymbol? themeType = compilation.GetTypeByMetadataName("Soenneker.Quark.Theme");
                if (themeType is null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(_themeTypeMissing, Location.None));
                    return;
                }

                INamedTypeSymbol? serviceProviderType = compilation.GetTypeByMetadataName("System.IServiceProvider");
                var entries = new List<(string ThemeTypeName, string OutputPath, bool BuildUnminified, bool BuildMinified)>(capacity: suiteCandidates.Length + generatorCandidates.Length);

                foreach (Candidate candidate in MergeCandidates(suiteCandidates, generatorCandidates))
                {
                    INamedTypeSymbol classSymbol = candidate.ClassSymbol;
                    AttributeData attributeData = candidate.Attribute;
                    Location? classLocation = classSymbol.Locations.FirstOrDefault();

                    string? outputFilePath = GetOutputFilePath(attributeData);
                    if (outputFilePath is null || outputFilePath.Trim().Length == 0)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(_missingOutputPath, classLocation, classSymbol.Name));
                        continue;
                    }

                    if (!TryGetThemeFactoryMember(classSymbol, themeType, serviceProviderType))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(_themeFactoryMissing, classLocation, classSymbol.Name));
                        continue;
                    }

                    string fullName = GetFullyQualifiedName(classSymbol);
                    (bool buildUnminified, bool buildMinified) = GetBuildUnminifiedAndMinified(attributeData);
                    entries.Add((fullName, outputFilePath.Trim(), buildUnminified, buildMinified));
                }

                EmitManifest(spc, entries);
            });
        }

        private static IEnumerable<Candidate> MergeCandidates(
            ImmutableArray<Candidate> suiteCandidates,
            ImmutableArray<Candidate> generatorCandidates)
        {
            if (suiteCandidates.IsDefaultOrEmpty && generatorCandidates.IsDefaultOrEmpty)
                return Array.Empty<Candidate>();

            var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var merged = new List<Candidate>(suiteCandidates.Length + generatorCandidates.Length);

            AddCandidates(suiteCandidates, merged, seen);
            AddCandidates(generatorCandidates, merged, seen);

            return merged;
        }

        private static void AddCandidates(
            ImmutableArray<Candidate> candidates,
            List<Candidate> merged,
            HashSet<INamedTypeSymbol> seen)
        {
            if (candidates.IsDefaultOrEmpty)
                return;

            foreach (Candidate candidate in candidates)
            {
                if (seen.Add(candidate.ClassSymbol))
                    merged.Add(candidate);
            }
        }

        private static bool TryGetThemeFactoryMember(INamedTypeSymbol classSymbol, INamedTypeSymbol themeType, INamedTypeSymbol? serviceProviderType)
        {
            var matches = 0;

            foreach (ISymbol? member in classSymbol.GetMembers())
            {
                if (member is IPropertySymbol property &&
                    property.DeclaredAccessibility == Accessibility.Public &&
                    property.IsStatic &&
                    property.GetMethod != null &&
                    SymbolEqualityComparer.Default.Equals(property.Type, themeType))
                {
                    matches++;
                    continue;
                }

                if (member is IMethodSymbol method &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    method.IsStatic &&
                    IsValidFactoryMethod(method, serviceProviderType) &&
                    SymbolEqualityComparer.Default.Equals(method.ReturnType, themeType))
                {
                    matches++;
                }
            }

            return matches == 1;
        }

        private static bool IsValidFactoryMethod(IMethodSymbol method, INamedTypeSymbol? serviceProviderType)
        {
            if (method.Parameters.Length == 0)
                return true;

            if (method.Parameters.Length != 1 || serviceProviderType is null)
                return false;

            return SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, serviceProviderType);
        }

        private static void EmitManifest(SourceProductionContext context, List<(string ThemeTypeName, string OutputPath, bool BuildUnminified, bool BuildMinified)> entries)
        {
            // Each line: {ThemeTypeName}|{OutputPath}|{BuildUnminified}|{BuildMinified}
            // Keep it stable + dead simple to parse at runtime.
            var sb = new StringBuilder(capacity: 256);

            for (var i = 0; i < entries.Count; i++)
            {
                (string themeTypeName, string outputPath, bool buildUnminified, bool buildMinified) = entries[i];

                if (themeTypeName.Length == 0 || outputPath.Length == 0)
                    continue;

                // Guard against accidental '|' or newlines in the path; you can make this stricter if you want.
                if (outputPath.IndexOf('|') >= 0 || outputPath.IndexOf('\n') >= 0 || outputPath.IndexOf('\r') >= 0)
                    continue;

                sb.Append(themeTypeName);
                sb.Append('|');
                sb.Append(outputPath);
                sb.Append('|');
                sb.Append(buildUnminified ? "1" : "0");
                sb.Append('|');
                sb.Append(buildMinified ? "1" : "0");
                sb.Append('\n');
            }

            string data = EscapeVerbatimString(sb.ToString());

            string source =
                "// <auto-generated />\n" +
                "namespace Soenneker.Quark.Gen.Themes.Generated\n" +
                "{\n" +
                "    internal static class QuarkThemeCssManifest\n" +
                "    {\n" +
                "        /// <summary>Each line: {ThemeTypeName}|{OutputPath}|{BuildUnminified}|{BuildMinified}</summary>\n" +
                $"        public const string Data = @\"{data}\";\n" +
                "    }\n" +
                "}\n";

            context.AddSource("QuarkThemeCssManifest.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private static string GetFullyQualifiedName(INamedTypeSymbol classSymbol)
        {
            string display = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return display.Replace("global::", string.Empty);
        }

        private static string EscapeVerbatimString(string value) =>
            value.Replace("\"", "\"\"");

        private static string? GetOutputFilePath(AttributeData attributeData)
        {
            if (attributeData.ConstructorArguments.Length > 0)
                return attributeData.ConstructorArguments[0].Value as string;

            foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "OutputFilePath" && namedArgument.Value.Value is string value)
                    return value;
            }

            return null;
        }

        private static (bool BuildUnminified, bool BuildMinified) GetBuildUnminifiedAndMinified(AttributeData attributeData)
        {
            var buildUnminified = true;
            var buildMinified = true;

            foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "BuildUnminified" && namedArgument.Value.Value is bool u)
                    buildUnminified = u;
                else if (namedArgument.Key == "BuildMinified" && namedArgument.Value.Value is bool m)
                    buildMinified = m;
            }

            return (buildUnminified, buildMinified);
        }
    }
}
