using System.Threading;
using Cysharp.Threading.Tasks;

namespace CH013.Commons
{
    public interface IFactory<T>
    {
        UniTask<T> Create(ICreateParameters parameters, CancellationToken cancellationToken);
    }
}
