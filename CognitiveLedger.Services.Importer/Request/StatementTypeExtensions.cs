using CognitiveLedger.Common.Types;

namespace CognitiveLedger.Services.Importer.Request;

public static class StatementTypeExtensions
{
    public static string ToCode(this StatementType statementType) => statementType switch
    {
        StatementType.Unknown => StatementTypeCatalog.UnknownCode,
        StatementType.BankAccount => StatementTypeCatalog.BankAccountCode,
        StatementType.CreditCard => StatementTypeCatalog.CreditCardCode,
        StatementType.RetailAccount => StatementTypeCatalog.RetailAccountCode,
        StatementType.MedicalBill => StatementTypeCatalog.MedicalBillCode,
        StatementType.UtilityBill => StatementTypeCatalog.UtilityBillCode,
        StatementType.Other => StatementTypeCatalog.OtherCode,
        _ => throw new ArgumentOutOfRangeException(nameof(statementType), statementType, "Unsupported statement type.")
    };
}
