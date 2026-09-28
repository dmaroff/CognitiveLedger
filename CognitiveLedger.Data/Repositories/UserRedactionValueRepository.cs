using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CognitiveLedger.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Data.Repositories;

public sealed class UserRedactionValueRepository(CognitiveLedgerDbContext dbContext)
    : IUserRedactionValueRepository
{
    public async Task<IReadOnlyList<string>> GetActiveValuesAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        var values = await dbContext.UserRedactionValues
            .AsNoTracking()
            .Where(redactionValue =>
                redactionValue.UserId == userId &&
                redactionValue.IsActive &&
                !redactionValue.IsDeleted)
            .Select(redactionValue => redactionValue.Value)
            .ToListAsync(cancellationToken);

        return
        [
            .. values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }
}