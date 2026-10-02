using System;
using Microsoft.CodeAnalysis;

namespace Soenneker.Quark.Gen.Themes;

internal readonly struct Candidate : IEquatable<Candidate>
{
    public string TypeName { get; }
    public string ClassName { get; }
    public string OutputPath { get; }
    public bool BuildUnminified { get; }
    public bool BuildMinified { get; }
    public string TailwindOutputPath { get; }
    public bool BuildTailwind { get; }
    public CandidateError Error { get; }
    public Location? Location { get; }

    public Candidate(string typeName, string className, string outputPath, bool buildUnminified, bool buildMinified,
        string tailwindOutputPath, bool buildTailwind, CandidateError error, Location? location)
    {
        TypeName = typeName;
        ClassName = className;
        OutputPath = outputPath;
        BuildUnminified = buildUnminified;
        BuildMinified = buildMinified;
        TailwindOutputPath = tailwindOutputPath;
        BuildTailwind = buildTailwind;
        Error = error;
        Location = location;
    }

    public bool Equals(Candidate other) => TypeName == other.TypeName && ClassName == other.ClassName && OutputPath == other.OutputPath &&
        BuildUnminified == other.BuildUnminified && BuildMinified == other.BuildMinified && TailwindOutputPath == other.TailwindOutputPath &&
        BuildTailwind == other.BuildTailwind && Error == other.Error && Equals(Location, other.Location);

    public override bool Equals(object? obj) => obj is Candidate other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(TypeName);
}
