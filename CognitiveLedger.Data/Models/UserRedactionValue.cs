using System.ComponentModel.DataAnnotations;
using CognitiveLedger.Data.Models.Base;

namespace CognitiveLedger.Data.Models;

public sealed class UserRedactionValue : FullModelBase
{
    public long UserId { get; init; }
    public User User { get; init; } = null!;

    [MaxLength(1000)]
    public required string Value { get; init; }
}
