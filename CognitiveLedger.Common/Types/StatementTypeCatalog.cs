namespace CognitiveLedger.Common.Types;

public static class StatementTypeCatalog
{
    public const long UnknownId = 1;
    public const long BankAccountId = 2;
    public const long CreditCardId = 3;
    public const long RetailAccountId = 4;
    public const long MedicalBillId = 5;
    public const long UtilityBillId = 6;
    public const long OtherId = 7;

    public const string UnknownCode = "UNKNOWN";
    public const string BankAccountCode = "BKACT";
    public const string CreditCardCode = "CRDCRD";
    public const string RetailAccountCode = "RTLACC";
    public const string MedicalBillCode = "MEDBIL";
    public const string UtilityBillCode = "UTLBIL";
    public const string OtherCode = "OTHER";
}
