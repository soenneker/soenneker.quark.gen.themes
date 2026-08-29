using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Quark.Gen.Themes.BuildTasks.Abstract
{
    /// <summary>
    /// Defines the quark theme write css runner contract.
    /// </summary>
    public interface IQuarkThemeWriteCssRunner
    {
        /// <summary>
        /// Runs quark Theme Write CSS Runner for the Quark Theme Write CSS Runner.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task whose result is the requested value.</returns>
        ValueTask<int> Run(string[] args, CancellationToken cancellationToken);
    }
}
