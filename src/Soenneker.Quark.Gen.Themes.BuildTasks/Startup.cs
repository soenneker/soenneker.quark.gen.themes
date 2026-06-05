using Microsoft.Extensions.DependencyInjection;
using Soenneker.Css.Minify.Registrars;
using Soenneker.Quark.Gen.Themes.BuildTasks.Abstract;
using Soenneker.Utils.Directory.Registrars;

namespace Soenneker.Quark.Gen.Themes.BuildTasks;

/// <summary>
/// Console type startup
/// </summary>
public static class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    /// <summary>
    /// Configures services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.SetupIoC();
    }

    /// <summary>
    /// Sets up io c.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection SetupIoC(this IServiceCollection services)
    {
        services.AddCssMinifierAsSingleton();
        services.AddDirectoryUtilAsSingleton();
        services.AddSingleton<IQuarkThemeWriteCssRunner, QuarkThemeWriteCssRunner>();

        services.AddHostedService<ConsoleHostedService>();

        return services;
    }
}