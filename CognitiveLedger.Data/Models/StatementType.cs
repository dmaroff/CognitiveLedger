using System.Collections.Generic;
using CognitiveLedger.Data.Models.CreditCard;

namespace CognitiveLedger.Data.Models;

public sealed class StatementType
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<CreditCardStatement> Statements { get; set; } = [];
}
