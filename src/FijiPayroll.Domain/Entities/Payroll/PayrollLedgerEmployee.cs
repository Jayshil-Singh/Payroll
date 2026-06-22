using FijiPayroll.Domain.Entities.Common;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Detail line mapping an individual employee's calculations inside the immutable ledger.
/// </summary>
public sealed class PayrollLedgerEmployee : BaseEntity
{
    public int CompanyId { get; private set; }
    public int PayrollLedgerId { get; private set; }
    public int EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public string EmployeeTin { get; private set; } = string.Empty;
    public string EmployeeFnpfNumber { get; private set; } = string.Empty;
    public decimal Gross { get; private set; }
    public decimal PAYE { get; private set; }
    public decimal FNPFEmployee { get; private set; }
    public decimal FNPFEmployer { get; private set; }
    public decimal NetPay { get; private set; }
    public string Hash { get; private set; } = string.Empty;

    private readonly List<PayrollLedgerComponent> _components = [];
    public IReadOnlyCollection<PayrollLedgerComponent> Components => _components.AsReadOnly();

    private PayrollLedgerEmployee() { } // For EF Core

    public static PayrollLedgerEmployee Create(
        int companyId,
        int employeeId,
        string employeeName,
        string employeeTin,
        string employeeFnpfNumber,
        decimal gross,
        decimal paye,
        decimal fnpfEmployee,
        decimal fnpfEmployer,
        decimal netPay,
        string hash)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId));
        if (string.IsNullOrWhiteSpace(employeeName)) throw new ArgumentException("Employee name is required.", nameof(employeeName));
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Integrity hash is required.", nameof(hash));

        return new PayrollLedgerEmployee
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            EmployeeTin = employeeTin ?? string.Empty,
            EmployeeFnpfNumber = employeeFnpfNumber ?? string.Empty,
            Gross = gross,
            PAYE = paye,
            FNPFEmployee = fnpfEmployee,
            FNPFEmployer = fnpfEmployer,
            NetPay = netPay,
            Hash = hash
        };
    }

    public void AddComponent(PayrollLedgerComponent component)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));
        _components.Add(component);
    }
}
