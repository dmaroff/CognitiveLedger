namespace CognitiveLedger.Common.Types;

public enum TransactionType
{
    Purchase,
    Payment,
    Refund,
    Credit,
    Fee,
    Interest,
    CashAdvance,
    BalanceTransfer,
    Adjustment,
    Unknown
}