using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Soenneker.Quark.Gen.Themes.CssWriter;

internal sealed class ProbingLoadContext : AssemblyLoadContext
{
    private readonly string _targetDir;
    private readonly string _frameworkDir;

    public ProbingLoadContext(string targetDir)
    {
        _targetDir = targetDir;
        _frameworkDir = Path.Combine(targetDir, "wwwroot", "_framework");
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var candidate = Path.Combine(_targetDir, name + ".dll");
        if (File.Exists(candidate))
            return LoadFromAssemblyPath(candidate);

        candidate = Path.Combine(_frameworkDir, name + ".dll");
        if (File.Exists(candidate))
            return LoadFromAssemblyPath(candidate);

        return null;
    }
}
