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
    public static void ConfigureServices(IServiceCollection services)
    {
        services.SetupIoC();
    }

    public static IServiceCollection SetupIoC(this IServiceCollection services)
    {
        services.AddCssMinifierAsScoped();
        services.AddDirectoryUtilAsScoped();
        services.AddScoped<IQuarkThemeWriteCssRunner, QuarkThemeWriteCssRunner>();

        services.AddHostedService<ConsoleHostedService>();

        return services;
    }
}