using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CognitiveLedger.Data.Models.Base;
using CognitiveLedger.Data.Models.CreditCard;

namespace CognitiveLedger.Data.Models;

public sealed class User : FullModelBase
{
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Address { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    [MaxLength(320)]
    public string EmailAddress { get; init; } = string.Empty;

    public ICollection<CreditCardStatement> Statements { get; init; } = [];
    public ICollection<StatementProcessingAudit> ProcessingAudits { get; init; } = [];
}

public static class UserCatalog
{
    public const long SystemUserId = 1;
}
