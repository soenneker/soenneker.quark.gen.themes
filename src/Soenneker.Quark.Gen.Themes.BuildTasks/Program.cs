using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soenneker.Quark.Gen.Themes.BuildTasks.Dtos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Quark.Gen.Themes.BuildTasks;

public sealed class Program
{
    private static CancellationTokenSource? _cts;

    public static async Task Main(string[] args)
    {
        _cts = new CancellationTokenSource();
        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            await CreateHostBuilder(args).RunConsoleAsync(_cts.Token);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Stopped program because of exception: {e}");
            throw;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress; // Detach the handler

            _cts.Dispose();
        }
    }

    /// <summary>
    /// Used for WebApplicationFactory, cannot delete, cannot change access, cannot change number of parameters.
    /// </summary>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        IHostBuilder host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, builder) =>
            {
                builder.AddEnvironmentVariables();
                builder.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);

                builder.Build();
            })
            .ConfigureLogging(logging =>
            {
                // Avoid EventLog provider dependency in build task execution
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(new CommandLineArgs(args));
                Startup.ConfigureServices(services);
            });

        return host;
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
    {
        eventArgs.Cancel = true; // Prevents immediate termination
        _cts?.Cancel();
    }
}