using System;
using System.Collections.Generic;
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
        private static readonly DiagnosticDescriptor MissingOutputPath = new(
            "QTG001",
            "Missing output file path",
            "GenerateQuarkThemeCssAttribute on '{0}' requires a non-empty output file path",
            "QuarkThemeGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ThemeFactoryMissing = new(
            "QTG002",
            "Theme factory missing",
            "GenerateQuarkThemeCssAttribute on '{0}' requires a single public static method or property returning Soenneker.Quark.Theme",
            "QuarkThemeGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ThemeTypeMissing = new(
            "QTG004",
            "Theme type missing",
            "Unable to locate Soenneker.Quark.Theme in referenced assemblies",
            "QuarkThemeGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var themeClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Soenneker.Quark.Suite.Attributes.GenerateQuarkThemeCssAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) =>
                {
                    var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
                    var attribute = ctx.Attributes.First();
                    return new Candidate(classSymbol, attribute);
                });

            var combined = context.CompilationProvider.Combine(themeClasses.Collect());

            context.RegisterSourceOutput(combined, static (spc, data) =>
            {
                var compilation = data.Left;
                var candidates = data.Right;

                if (candidates.IsDefaultOrEmpty)
                    return;

                var themeType = compilation.GetTypeByMetadataName("Soenneker.Quark.Theme");
                if (themeType == null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(ThemeTypeMissing, Location.None));
                    return;
                }

                foreach (var candidate in candidates)
                {
                    var classSymbol = candidate.ClassSymbol;
                    var attributeData = candidate.Attribute;
                    var classLocation = classSymbol.Locations.FirstOrDefault();

                    var outputFilePath = GetOutputFilePath(attributeData);
                    if (outputFilePath == null)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(MissingOutputPath, classLocation, classSymbol.Name));
                        continue;
                    }

                    var trimmedOutputFilePath = outputFilePath.Trim();
                    if (trimmedOutputFilePath.Length == 0)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(MissingOutputPath, classLocation, classSymbol.Name));
                        continue;
                    }

                    if (!TryGetThemeFactoryMember(classSymbol, themeType))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(ThemeFactoryMissing, classLocation, classSymbol.Name));
                        continue;
                    }

                    EmitCssArtifact(spc, classSymbol, trimmedOutputFilePath);
                }
            });
        }

        private static bool TryGetThemeFactoryMember(INamedTypeSymbol classSymbol, INamedTypeSymbol themeType)
        {
            var matches = new List<ISymbol>();

            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IPropertySymbol property &&
                    property.DeclaredAccessibility == Accessibility.Public &&
                    property.IsStatic &&
                    property.GetMethod != null &&
                    SymbolEqualityComparer.Default.Equals(property.Type, themeType))
                {
                    matches.Add(property);
                }

                if (member is IMethodSymbol method &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    method.IsStatic &&
                    method.Parameters.Length == 0 &&
                    SymbolEqualityComparer.Default.Equals(method.ReturnType, themeType))
                {
                    matches.Add(method);
                }
            }

            return matches.Count == 1;
        }

        private static void EmitCssArtifact(SourceProductionContext context, INamedTypeSymbol classSymbol, string outputFilePath)
        {
            var fullName = GetFullyQualifiedName(classSymbol);
            var id = MakeIdentifier(classSymbol.Name) + "_" + ComputeStableHash(fullName);
            var hintName = $"QuarkThemeCssArtifact_{id}.g.cs";
            var escapedPath = EscapeVerbatimString(outputFilePath);

            var source =
                "// <auto-generated />\n" +
                "namespace Soenneker.Quark.Gen.Themes.Generated\n" +
                "{\n" +
                $"    internal static class QuarkThemeCssArtifact_{id}\n" +
                "    {\n" +
                $"        public const string OutputPath = @\"{escapedPath}\";\n" +
                $"        public const string ThemeTypeName = \"{fullName}\";\n" +
                "    }\n" +
                "}\n";

            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }

        private static string GetFullyQualifiedName(INamedTypeSymbol classSymbol)
        {
            var display = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return display.Replace("global::", string.Empty);
        }

        private static string EscapeVerbatimString(string value) =>
            value.Replace("\"", "\"\"");

        private static string MakeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Theme";

            var builder = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }

            if (builder.Length == 0 || !char.IsLetter(builder[0]) && builder[0] != '_')
                builder.Insert(0, '_');

            return builder.ToString();
        }

        private static string ComputeStableHash(string value)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;
                uint hash = fnvOffset;

                foreach (var ch in value)
                {
                    hash ^= ch;
                    hash *= fnvPrime;
                }

                return hash.ToString("X8");
            }
        }

        private static string? GetOutputFilePath(AttributeData attributeData)
        {
            if (attributeData.ConstructorArguments.Length > 0)
                return attributeData.ConstructorArguments[0].Value as string;

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "OutputFilePath" && namedArgument.Value.Value is string value)
                    return value;
            }

            return null;
        }

        private readonly struct Candidate
        {
            public INamedTypeSymbol ClassSymbol { get; }
            public AttributeData Attribute { get; }

            public Candidate(INamedTypeSymbol classSymbol, AttributeData attribute)
            {
                ClassSymbol = classSymbol;
                Attribute = attribute;
            }
        }
    }
}
