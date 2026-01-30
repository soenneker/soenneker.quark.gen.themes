using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Quark.Gen.Themes.BuildTasks.Abstract
{
    public interface IQuarkThemeWriteCssRunner
    {
        ValueTask<int> Run(string[] args, CancellationToken cancellationToken);
    }
}
