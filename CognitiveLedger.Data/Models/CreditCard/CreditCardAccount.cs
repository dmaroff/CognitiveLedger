using System;
using System.Collections.Generic;
using CognitiveLedger.Data.Models.Base;
// ReSharper disable All

namespace CognitiveLedger.Data.Models.CreditCard;

public class CreditCardAccount : FullModelBase
{
    public string IssuerName { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string LastFourDigits { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";

    public List<CreditCardStatement> Statements { get; set; } = [];
}