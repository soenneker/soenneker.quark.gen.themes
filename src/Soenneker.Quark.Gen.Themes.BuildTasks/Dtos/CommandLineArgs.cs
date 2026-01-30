namespace Soenneker.Quark.Gen.Themes.BuildTasks.Dtos;

/// <summary>
/// Holds the command-line arguments passed to Main so the runner receives exactly those (e.g. --targetPath, --projectDir)
/// instead of the full process command line from Environment.GetCommandLineArgs().
/// </summary>
public sealed class CommandLineArgs
{
    public string[] Args { get; }

    public CommandLineArgs(string[]? args)
    {
        Args = args ?? [];
    }
}
