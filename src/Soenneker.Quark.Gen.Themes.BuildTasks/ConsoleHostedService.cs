using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soenneker.Quark.Gen.Themes.BuildTasks.Abstract;
using Soenneker.Quark.Gen.Themes.BuildTasks.Dtos;

namespace Soenneker.Quark.Gen.Themes.BuildTasks;

public sealed class ConsoleHostedService : IHostedService
{
    private readonly ILogger<ConsoleHostedService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IQuarkThemeWriteCssRunner _runner;
    private readonly CommandLineArgs _commandLineArgs;

    private int? _exitCode;

    public ConsoleHostedService(ILogger<ConsoleHostedService> logger, IHostApplicationLifetime appLifetime, IQuarkThemeWriteCssRunner runner, 
        CommandLineArgs commandLineArgs)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _runner = runner;
        _commandLineArgs = commandLineArgs;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                _logger.LogInformation("Starting Soenneker.Quark.Gen.Themes.BuildTasks...");

                try
                {
                    _exitCode = await _runner.Run(_commandLineArgs.Args, cancellationToken).AsTask();
                }
                catch (Exception e)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();

                    _logger.LogError(e, "Unhandled exception");

                    await Task.Delay(2000, cancellationToken);
                    _exitCode = 1;
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            }, cancellationToken);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Exit code may be null if the user cancelled via Ctrl+C/SIGTERM
        Environment.ExitCode = _exitCode.GetValueOrDefault(-1);
        _logger.LogInformation("Stopping Soenneker.Quark.Gen.Themes.BuildTasks with exit code {ExitCode}.", Environment.ExitCode);
        return Task.CompletedTask;
    }
}