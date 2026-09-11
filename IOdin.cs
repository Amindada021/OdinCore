using System.Threading;
using System.Threading.Tasks;

namespace OdinCore;

public interface IOdin<TRequest, TResponse>
{
    Result<TResponse> RunExecute(TRequest? request);

    Task<Result<TResponse>> RunExecuteAsync(TRequest? request);

    Task<Result<TResponse>> RunExecuteAsync(TRequest? request, CancellationToken cancellationToken);
}
