using CognitiveLedger.Common.Types;
using CognitiveLedger.Statements.Abstractions;

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

    public static StatementKind ToStatementKind(this StatementType statementType) =>
        statementType switch
        {
            StatementType.BankAccount => StatementKind.BankAccount,
            StatementType.CreditCard => StatementKind.CreditCard,
            StatementType.RetailAccount => StatementKind.RetailAccount,
            StatementType.MedicalBill => StatementKind.MedicalBill,
            StatementType.UtilityBill => StatementKind.UtilityBill,
            StatementType.Other => StatementKind.Other,
            _ => StatementKind.Unknown
        };
}
