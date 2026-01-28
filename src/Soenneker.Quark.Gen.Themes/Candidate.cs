using Microsoft.CodeAnalysis;

namespace Soenneker.Quark.Gen.Themes;

internal readonly struct Candidate
{
    public INamedTypeSymbol ClassSymbol { get; }
    public AttributeData Attribute { get; }

    public Candidate(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        ClassSymbol = classSymbol;
        Attribute = attribute;
    }
}
