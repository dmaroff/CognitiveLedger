namespace CognitiveLedger.Statements.Abstractions;

public enum StatementKind
{
    Unknown = 0,
    BankAccount = 1,
    CreditCard = 2,
    RetailAccount = 3,
    MedicalBill = 4,
    UtilityBill = 5,
    Other = 6
}
