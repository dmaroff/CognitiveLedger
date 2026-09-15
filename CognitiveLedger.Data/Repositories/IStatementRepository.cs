using System.Threading;
using System.Threading.Tasks;
using CognitiveLedger.Data.Models.CreditCard;

namespace CognitiveLedger.Data.Repositories;

public interface IStatementRepository
{
    Task<CreditCardStatement?> FindBySourceDocumentSha256Async(
        string sourceDocumentSha256,
        CancellationToken cancellationToken = default);

    Task<CreditCardStatement> InsertStatementAsync(
        CreditCardStatement statement,
        CancellationToken cancellationToken = default);
}
