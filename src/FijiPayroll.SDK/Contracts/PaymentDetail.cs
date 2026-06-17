using System;

namespace FijiPayroll.SDK.Contracts;

/// <summary>
/// Shared data transfer object representing a single bank transaction detail.
/// Passed to bank direct-credit generators.
/// </summary>
public sealed record PaymentDetail(
    int EmployeeId,
    string EmployeeName,
    string Bsb = "",
    string AccountNumber = "",
    decimal Amount = 0,
    string Reference = "",
    string Tin = "",
    decimal Gross = 0,
    decimal Paye = 0,
    string FnpfNumber = "",
    decimal FnpfEmployee = 0,
    decimal FnpfEmployer = 0,
    string BankAccountNumber = "",
    string EmployeeCode = ""
);
