using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Soenneker.Quark.Gen.Themes.BuildTasks
{
    public sealed class QuarkThemeWriteCss : Task
    {
        [Required]
        public string GeneratedFilesPath { get; set; } = string.Empty;

        [Required]
        public string ProjectDir { get; set; } = string.Empty;

        [Required]
        public string TargetPath { get; set; } = string.Empty;

        [Required]
        public string TargetDir { get; set; } = string.Empty;

        public override bool Execute()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GeneratedFilesPath))
                    return true;

                if (!Path.IsPathRooted(GeneratedFilesPath))
                    GeneratedFilesPath = Path.GetFullPath(Path.Combine(ProjectDir, GeneratedFilesPath));

                if (!Directory.Exists(GeneratedFilesPath))
                    return true;

                if (string.IsNullOrWhiteSpace(TargetPath))
                    return true;

                var resolvedTargetPath = ResolveTargetPath();
                if (resolvedTargetPath == null || !File.Exists(resolvedTargetPath))
                    return true;

                var outputPathRegex = new Regex("OutputPath\\s*=\\s*@\"(?<path>(?:[^\"]|\"\")*)\";", RegexOptions.Compiled);
                var themeTypeRegex = new Regex("ThemeTypeName\\s*=\\s*\"(?<type>[^\"]*)\";", RegexOptions.Compiled);

                ResolveEventHandler? handler = (_, args) =>
                {
                    var name = new AssemblyName(args.Name).Name;
                    if (string.IsNullOrWhiteSpace(name))
                        return null;

                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                            return assembly;
                    }

                    foreach (var directory in GetAssemblySearchDirectories())
                    {
                        var candidate = Path.Combine(directory, name + ".dll");
                        if (File.Exists(candidate))
                            return LoadAssemblyFromBytes(candidate);
                    }

                    return null;
                };

                AppDomain.CurrentDomain.AssemblyResolve += handler;

                try
                {
                    var projectAssembly = LoadAssemblyFromBytes(resolvedTargetPath);
                    if (projectAssembly == null)
                        return true;

                    var componentsGeneratorType = FindType("Soenneker.Quark.ComponentsCssGenerator");
                    var bootstrapGeneratorType = FindType("Soenneker.Quark.BootstrapCssGenerator");

                    if (componentsGeneratorType == null || bootstrapGeneratorType == null)
                    {
                        Log.LogWarning("QuarkThemeWriteCss: Unable to locate required generator types. Ensure Soenneker.Quark.Suite assemblies are available in the build output.");
                        return true;
                    }

                    var artifactFiles = new List<string>(Directory.GetFiles(GeneratedFilesPath, "QuarkThemeCssArtifact_*.g.cs", SearchOption.AllDirectories));

                    if (artifactFiles.Count == 0)
                    {
                        var objDir = Path.Combine(ProjectDir, "obj");
                        if (Directory.Exists(objDir))
                            artifactFiles.AddRange(Directory.GetFiles(objDir, "QuarkThemeCssArtifact_*.g.cs", SearchOption.AllDirectories));
                    }

                    foreach (var file in artifactFiles)
                    {
                        var text = File.ReadAllText(file);
                        if (text.IndexOf("QuarkThemeCssArtifact_", StringComparison.Ordinal) < 0)
                            continue;

                        var outputMatch = outputPathRegex.Match(text);
                        var themeTypeMatch = themeTypeRegex.Match(text);

                        if (!outputMatch.Success || !themeTypeMatch.Success)
                            continue;

                        var outputPath = outputMatch.Groups["path"].Value.Replace("\"\"", "\"");
                        if (!Path.IsPathRooted(outputPath))
                            outputPath = Path.GetFullPath(Path.Combine(ProjectDir, outputPath));

                        var themeTypeName = themeTypeMatch.Groups["type"].Value;
                        if (string.IsNullOrWhiteSpace(themeTypeName))
                            continue;

                        var classType = projectAssembly.GetType(themeTypeName, throwOnError: false);
                        if (classType == null)
                        {
                            Log.LogWarning($"QuarkThemeWriteCss: Theme type '{themeTypeName}' not found in target assembly.");
                            continue;
                        }

                        var themeMember = FindThemeFactoryMember(classType);
                        if (themeMember == null)
                        {
                            Log.LogWarning($"QuarkThemeWriteCss: Theme type '{themeTypeName}' does not expose a single public static factory returning Soenneker.Quark.Theme.");
                            continue;
                        }

                        var themeInstance = themeMember switch
                        {
                            PropertyInfo property => property.GetValue(null),
                            MethodInfo method => method.Invoke(null, null),
                            _ => null
                        };

                        if (themeInstance == null)
                            continue;

                        var css = GenerateCss(themeInstance, componentsGeneratorType, bootstrapGeneratorType);
                        if (css == null)
                            continue;

                        var directory = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        if (File.Exists(outputPath))
                        {
                            var existing = File.ReadAllText(outputPath);
                            if (string.Equals(existing, css, StringComparison.Ordinal))
                                continue;
                        }

                        File.WriteAllText(outputPath, css, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    }
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= handler;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
        }

        private Type? FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Skip assemblies that cannot resolve type metadata.
                }
            }

            foreach (var directory in GetAssemblySearchDirectories())
            {
                if (!Directory.Exists(directory))
                    continue;

                foreach (var dll in Directory.GetFiles(directory, "Soenneker.Quark*.dll"))
                {
                    try
                    {
                        var assembly = LoadAssemblyFromBytes(dll);
                        if (assembly == null)
                            continue;
                        var type = assembly.GetType(fullName, throwOnError: false);
                        if (type != null)
                            return type;
                    }
                    catch
                    {
                        // Best-effort resolution.
                    }
                }
            }

            return null;
        }

        private static MemberInfo? FindThemeFactoryMember(Type classType)
        {
            var members = new List<MemberInfo>();

            foreach (var property in classType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.GetMethod != null && IsThemeType(property.PropertyType))
                    members.Add(property);
            }

            foreach (var method in classType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (IsThemeType(method.ReturnType) && method.GetParameters().Length == 0)
                    members.Add(method);
            }

            return members.Count == 1 ? members[0] : null;
        }

        private static Assembly? LoadAssemblyFromBytes(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return Assembly.Load(memory.ToArray());
            }
            catch
            {
                return null;
            }
        }

        private static bool IsThemeType(Type type) =>
            string.Equals(type.FullName, "Soenneker.Quark.Theme", StringComparison.Ordinal);

        private IEnumerable<string> GetAssemblySearchDirectories()
        {
            if (!string.IsNullOrWhiteSpace(TargetDir))
                yield return TargetDir;

            var frameworkDir = Path.Combine(TargetDir, "wwwroot", "_framework");
            if (Directory.Exists(frameworkDir))
                yield return frameworkDir;
        }

        private string? ResolveTargetPath()
        {
            if (File.Exists(TargetPath))
                return TargetPath;

            var fileName = Path.GetFileName(TargetPath);
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            foreach (var directory in GetAssemblySearchDirectories())
            {
                var candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string? GenerateCss(
            object themeInstance,
            Type componentsGeneratorType,
            Type bootstrapGeneratorType)
        {
            var cssParts = new List<string>(2);

            var themeType = themeInstance.GetType();
            var componentsMethod = FindSingleArgumentMethod(componentsGeneratorType, "Generate", themeType);
            var componentsCss = componentsMethod?.Invoke(null, new[] { themeInstance }) as string;
            if (!string.IsNullOrWhiteSpace(componentsCss))
                cssParts.Add(componentsCss!);

            var bootstrapProperty = themeType.GetProperty("BootstrapCssVariables", BindingFlags.Public | BindingFlags.Instance);
            var bootstrapVariables = bootstrapProperty?.GetValue(themeInstance);

            if (bootstrapVariables != null)
            {
                var bootstrapMethod = FindSingleArgumentMethod(bootstrapGeneratorType, "Generate", bootstrapVariables.GetType());
                var bootstrapCss = bootstrapMethod?.Invoke(null, new[] { bootstrapVariables }) as string;

                if (!string.IsNullOrWhiteSpace(bootstrapCss))
                    cssParts.Add(bootstrapCss!);
            }

            if (cssParts.Count == 0)
                return string.Empty;

            return string.Join("\n\n", cssParts);
        }

        private static MethodInfo? FindSingleArgumentMethod(Type type, string methodName, Type argumentType)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                    continue;

                if (parameters[0].ParameterType.IsAssignableFrom(argumentType))
                    return method;
            }

            return null;
        }
    }
}
