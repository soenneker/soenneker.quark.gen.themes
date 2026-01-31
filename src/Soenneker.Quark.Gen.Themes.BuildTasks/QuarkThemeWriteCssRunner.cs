using Soenneker.Css.Minify.Abstract;
using Soenneker.Extensions.ValueTask;
using Soenneker.Quark.Gen.Themes.BuildTasks.Abstract;
using Soenneker.Quark.Gen.Themes.BuildTasks.Dtos;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.File.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Quark.Gen.Themes.BuildTasks;

///<inheritdoc cref="IQuarkThemeWriteCssRunner"/>
public class QuarkThemeWriteCssRunner : IQuarkThemeWriteCssRunner
{
    private const string _manifestTypeName = "Soenneker.Quark.Gen.Themes.Generated.QuarkThemeCssManifest";
    private const string _manifestFieldName = "Data";

    private readonly ICssMinifier _cssMinifier;
    private readonly IFileUtil _fileUtil;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IServiceProvider _services;

    public QuarkThemeWriteCssRunner(ICssMinifier cssMinifier, IFileUtil fileUtil, IDirectoryUtil directoryUtil, IServiceProvider services)
    {
        _cssMinifier = cssMinifier;
        _fileUtil = fileUtil;
        _directoryUtil = directoryUtil;
        _services = services;
    }

    public async ValueTask<int> Run(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            var map = ParseArgs(args);

            if (!map.TryGetValue("--targetPath", out var targetPath) || string.IsNullOrWhiteSpace(targetPath))
                return Fail("Missing required --targetPath");

            if (!map.TryGetValue("--projectDir", out var projectDir) || string.IsNullOrWhiteSpace(projectDir))
                return Fail("Missing required --projectDir");

            var buildUnminified = ParseBool(map, "--buildUnminified", defaultValue: true);
            var buildMinified = ParseBool(map, "--buildMinified", defaultValue: true);

            // When Windows cmd escapes the closing quote on projectDir, the rest of the args are swallowed
            // so --buildMinified may be missing from the map; we default to true, but log to diagnose.
            if (!map.ContainsKey("--buildUnminified") || !map.ContainsKey("--buildMinified"))
                Console.Error.WriteLine("[QuarkTheme] buildUnminified={0} buildMinified={1} (some args may have been swallowed by shell quoting)",
                    buildUnminified, buildMinified);

            if (!buildUnminified && !buildMinified)
                return 0; // nothing to write

            targetPath = SanitizePathArg(targetPath);
            projectDir = SanitizePathArg(projectDir);
            targetPath = Path.GetFullPath(targetPath);
            projectDir = Path.GetFullPath(projectDir);

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

                object? themeInstance = factory switch
                {
                    PropertyInfo p => p.GetValue(null),
                    MethodInfo m => InvokeThemeFactory(m, _services),
                    _ => null
                };

                if (themeInstance is null)
                    continue;

                var css = GenerateCss(themeInstance, componentsGen, bootstrapGen);
                if (css is null)
                    continue;

                var outputPath = entry.OutputPath;
                if (!Path.IsPathRooted(outputPath))
                    outputPath = Path.GetFullPath(Path.Combine(projectDir, outputPath));

                // Two files: base.css (unminified) and base.min.css (minified). Write each only when its flag is true.
                var unminifiedPath = GetBaseCssPath(outputPath);
                var minifiedPath = GetMinifiedPath(unminifiedPath);

                if (buildUnminified)
                    await AtomicWriteUtf8NoBom(unminifiedPath, css, cancellationToken);

                if (buildMinified)
                {
                    var minifiedCss = _cssMinifier.Minify(css);
                    await AtomicWriteUtf8NoBom(minifiedPath, minifiedCss, cancellationToken);
                }
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

    /// <summary>
    /// Strips garbage from path args when Windows cmd escapes the closing quote (e.g. projectDir becomes "...Demo\" --buildUnminified...").
    /// </summary>
    private static string SanitizePathArg(string value)
    {
        value = value.Trim();
        var quoteIdx = value.IndexOf('"');
        if (quoteIdx >= 0)
            value = value.Substring(0, quoteIdx);
        return value.Trim().Trim('"', '\\', ' ');
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

    private static bool ParseBool(IReadOnlyDictionary<string, string> map, string key, bool defaultValue)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return defaultValue;

        value = value.Trim();
        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1")
            return true;
        if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || value == "0")
            return false;

        return defaultValue;
    }

    /// <summary>
    /// Normalizes the path so it ends with .css (not .min.css). If manifest path is theme.min.css, returns theme.css.
    /// </summary>
    private static string GetBaseCssPath(string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath) ?? ".";
        var fileName = Path.GetFileName(outputPath);
        if (fileName.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase))
            fileName = fileName.Substring(0, fileName.Length - 7) + ".css"; // .min.css -> .css
        return Path.Combine(dir, fileName);
    }

    /// <summary>
    /// Returns the path for the minified file: base.css -> base.min.css.
    /// </summary>
    private static string GetMinifiedPath(string baseCssPath)
    {
        var dir = Path.GetDirectoryName(baseCssPath) ?? ".";
        var fileName = Path.GetFileName(baseCssPath);
        var lastDot = fileName.LastIndexOf('.');
        if (lastDot <= 0)
            return Path.Combine(dir, fileName + ".min");
        var nameWithoutExt = fileName.Substring(0, lastDot);
        var ext = fileName.Substring(lastDot);
        return Path.Combine(dir, nameWithoutExt + ".min" + ext);
    }

    private static string? ReadManifest(Assembly asm)
    {
        var type = asm.GetType(_manifestTypeName, throwOnError: false, ignoreCase: false);
        if (type is null)
            return null;

        // const string Data => emits as a literal field
        var field = type.GetField(_manifestFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
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
            if (!IsThemeFactoryMethod(method))
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

    private static bool IsThemeFactoryMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return true;

        if (parameters.Length != 1)
            return false;

        return typeof(IServiceProvider).IsAssignableFrom(parameters[0].ParameterType);
    }

    private static object? InvokeThemeFactory(MethodInfo method, IServiceProvider services)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return method.Invoke(null, null);

        if (parameters.Length == 1 && typeof(IServiceProvider).IsAssignableFrom(parameters[0].ParameterType))
            return method.Invoke(null, new object?[] { services });

        return null;
    }

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
        // Each line: ThemeTypeName|OutputPath|BuildUnminified|BuildMinified (or legacy: ThemeTypeName|OutputPath|MinifyCss)
        var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var firstSep = line.IndexOf('|');
            if (firstSep <= 0 || firstSep >= line.Length - 1)
                continue;

            var secondSep = line.IndexOf('|', firstSep + 1);
            var typeName = line.Substring(0, firstSep).Trim();
            var path = (secondSep == -1 ? line.Substring(firstSep + 1) : line.Substring(firstSep + 1, secondSep - firstSep - 1)).Trim();
            var buildUnminified = true;
            var buildMinified = true;

            if (secondSep != -1 && secondSep < line.Length - 1)
            {
                var rest = line.Substring(secondSep + 1);
                var thirdSep = rest.IndexOf('|');
                if (thirdSep >= 0)
                {
                    var u = ParseManifestBool(rest.Substring(0, thirdSep));
                    var m = ParseManifestBool(thirdSep + 1 < rest.Length ? rest.Substring(thirdSep + 1) : "");
                    buildUnminified = u ?? true;
                    buildMinified = m ?? true;
                }
            }

            if (typeName.Length == 0 || path.Length == 0)
                continue;

            yield return new ManifestEntry(typeName, path, buildUnminified, buildMinified);
        }
    }

    private static bool? ParseManifestBool(string value)
    {
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (string.Equals(value, "1", StringComparison.Ordinal) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(value, "0", StringComparison.Ordinal) || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            return false;
        return null;
    }

    private async ValueTask AtomicWriteUtf8NoBom(string path, string content, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(path);

        await _directoryUtil.CreateIfDoesNotExist(dir, true, cancellationToken).NoSync();

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path);
            if (string.Equals(existing, content, StringComparison.Ordinal))
                return;
        }

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await _fileUtil.Move(tmp, path, cancellationToken: cancellationToken).NoSync();
    }
}
