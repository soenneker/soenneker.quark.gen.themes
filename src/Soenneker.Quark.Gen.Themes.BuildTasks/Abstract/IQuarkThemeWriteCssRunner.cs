using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Quark.Gen.Themes.BuildTasks.Abstract
{
    /// <summary>
    /// Writes component and Tailwind CSS for themes declared in a compiled project.
    /// </summary>
    public interface IQuarkThemeWriteCssRunner
    {
        /// <summary>
        /// Writes the configured theme CSS outputs using the supplied build-task arguments.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The process exit code: zero on success; otherwise nonzero.</returns>
        ValueTask<int> Run(string[] args, CancellationToken cancellationToken);
    }
}
