using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Soenneker.Quark.Gen.Themes.CssWriter;

internal static class Program
{
    private const string ManifestTypeName = "Soenneker.Quark.Gen.Themes.Generated.QuarkThemeCssManifest";
    private const string ManifestFieldName = "Data";

    public static int Main(string[] args)
    {
        try
        {
            var map = ParseArgs(args);

            if (!map.TryGetValue("--targetPath", out var targetPath) || string.IsNullOrWhiteSpace(targetPath))
                return Fail("Missing required --targetPath");

            if (!map.TryGetValue("--projectDir", out var projectDir) || string.IsNullOrWhiteSpace(projectDir))
                return Fail("Missing required --projectDir");

            targetPath = Path.GetFullPath(targetPath.Trim().Trim('"'));
            projectDir = Path.GetFullPath(projectDir.Trim().Trim('"'));

            if (!File.Exists(targetPath))
                return Fail($"Target assembly not found: {targetPath}");

            var targetDir = Path.GetDirectoryName(targetPath) ?? projectDir;

            var loadContext = new ProbingLoadContext(targetDir);
            var asm = loadContext.LoadFromAssemblyPath(targetPath);

            // Load referenced assemblies (e.g. Soenneker.Quark.Suite) into the same context so generator types are found
            LoadReferencedAssemblies(loadContext, targetDir, asm.GetReferencedAssemblies());

            var manifest = ReadManifest(asm);
            if (string.IsNullOrWhiteSpace(manifest))
                return 0; // nothing to do

            var componentsGen = FindLoadedType(loadContext, "Soenneker.Quark.ComponentsCssGenerator");
            var bootstrapGen = FindLoadedType(loadContext, "Soenneker.Quark.BootstrapCssGenerator");

            if (componentsGen is null || bootstrapGen is null)
                return Fail("Missing required types: Soenneker.Quark.ComponentsCssGenerator / Soenneker.Quark.BootstrapCssGenerator");

            foreach (var entry in ParseManifest(manifest))
            {
                var themeType = asm.GetType(entry.ThemeTypeName, throwOnError: false, ignoreCase: false);
                if (themeType is null)
                    continue;

                var factory = FindSingleThemeFactory(themeType);
                if (factory is null)
                    continue;

                object? themeInstance = factory is PropertyInfo p ? p.GetValue(null)
                                     : factory is MethodInfo m ? m.Invoke(null, null)
                                     : null;

                if (themeInstance is null)
                    continue;

                var css = GenerateCss(themeInstance, componentsGen, bootstrapGen);
                if (css is null)
                    continue;

                var outputPath = entry.OutputPath;
                if (!Path.IsPathRooted(outputPath))
                    outputPath = Path.GetFullPath(Path.Combine(projectDir, outputPath));

                AtomicWriteUtf8NoBom(outputPath, css);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            if (string.IsNullOrWhiteSpace(key) || key[0] != '-')
                continue;

            if (i + 1 >= args.Length)
                break;

            var value = args[i + 1];
            if (string.IsNullOrWhiteSpace(value) || value[0] == '-')
                continue;

            map[key] = value;
            i++;
        }

        return map;
    }

    private static string? ReadManifest(Assembly asm)
    {
        var type = asm.GetType(ManifestTypeName, throwOnError: false, ignoreCase: false);
        if (type is null)
            return null;

        // const string Data => emits as a literal field
        var field = type.GetField(ManifestFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.FieldType == typeof(string))
            return field.GetRawConstantValue() as string;

        return null;
    }

    private static void LoadReferencedAssemblies(ProbingLoadContext context, string targetDir, IEnumerable<AssemblyName> refs)
    {
        var frameworkDir = Path.Combine(targetDir, "wwwroot", "_framework");

        foreach (var refName in refs)
        {
            var name = refName.Name;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var candidate = Path.Combine(targetDir, name + ".dll");
            if (File.Exists(candidate))
            {
                try { context.LoadFromAssemblyPath(candidate); } catch { /* ignore */ }
                continue;
            }

            candidate = Path.Combine(frameworkDir, name + ".dll");
            if (File.Exists(candidate))
            {
                try { context.LoadFromAssemblyPath(candidate); } catch { /* ignore */ }
            }
        }
    }

    private static Type? FindLoadedType(AssemblyLoadContext context, string fullName)
    {
        foreach (var a in context.Assemblies)
        {
            try
            {
                var t = a.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (t is not null)
                    return t;
            }
            catch
            {
                // ignore
            }
        }

        return null;
    }

    private static MemberInfo? FindSingleThemeFactory(Type themeClass)
    {
        MemberInfo? match = null;

        foreach (var prop in themeClass.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (prop.GetMethod is null)
                continue;

            if (!IsThemeType(prop.PropertyType))
                continue;

            if (match is not null)
                return null;

            match = prop;
        }

        foreach (var method in themeClass.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.GetParameters().Length != 0)
                continue;

            if (!IsThemeType(method.ReturnType))
                continue;

            if (match is not null)
                return null;

            match = method;
        }

        return match;
    }

    private static bool IsThemeType(Type t) =>
        string.Equals(t.FullName, "Soenneker.Quark.Theme", StringComparison.Ordinal);

    private static string? GenerateCss(object themeInstance, Type componentsGeneratorType, Type bootstrapGeneratorType)
    {
        var parts = new List<string>(2);

        var themeType = themeInstance.GetType();

        var componentsMethod = FindSingleArgMethod(componentsGeneratorType, "Generate", themeType);
        if (componentsMethod is not null)
        {
            var css = componentsMethod.Invoke(null, new[] { themeInstance }) as string;
            if (!string.IsNullOrWhiteSpace(css))
                parts.Add(css!);
        }

        var bootstrapVarsProp = themeType.GetProperty("BootstrapCssVariables", BindingFlags.Public | BindingFlags.Instance);
        var bootstrapVars = bootstrapVarsProp?.GetValue(themeInstance);

        if (bootstrapVars is not null)
        {
            var bootstrapMethod = FindSingleArgMethod(bootstrapGeneratorType, "Generate", bootstrapVars.GetType());
            if (bootstrapMethod is not null)
            {
                var css = bootstrapMethod.Invoke(null, new[] { bootstrapVars }) as string;
                if (!string.IsNullOrWhiteSpace(css))
                    parts.Add(css!);
            }
        }

        if (parts.Count == 0)
            return string.Empty;

        return string.Join("\n\n", parts);
    }

    private static MethodInfo? FindSingleArgMethod(Type type, string name, Type argType)
    {
        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (!string.Equals(m.Name, name, StringComparison.Ordinal))
                continue;

            var ps = m.GetParameters();
            if (ps.Length != 1)
                continue;

            if (ps[0].ParameterType.IsAssignableFrom(argType))
                return m;
        }

        return null;
    }

    private static IEnumerable<ManifestEntry> ParseManifest(string data)
    {
        // Each line: ThemeTypeName|OutputPath
        var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var sep = line.IndexOf('|');
            if (sep <= 0 || sep >= line.Length - 1)
                continue;

            var typeName = line.Substring(0, sep).Trim();
            var path = line.Substring(sep + 1).Trim();

            if (typeName.Length == 0 || path.Length == 0)
                continue;

            yield return new ManifestEntry(typeName, path);
        }
    }

    private static void AtomicWriteUtf8NoBom(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path);
            if (string.Equals(existing, content, StringComparison.Ordinal))
                return;
        }

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(tmp, path, overwrite: true);
    }
}
