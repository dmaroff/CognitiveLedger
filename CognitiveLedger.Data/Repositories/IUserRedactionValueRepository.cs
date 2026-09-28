using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CognitiveLedger.Data.Repositories;

public interface IUserRedactionValueRepository
{
    Task<IReadOnlyList<string>> GetActiveValuesAsync(
        long userId,
        CancellationToken cancellationToken = default);
}
