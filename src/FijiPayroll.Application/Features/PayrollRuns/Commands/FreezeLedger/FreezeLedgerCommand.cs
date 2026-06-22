using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services.EvidencePack;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.FreezeLedger;

public sealed record FreezeLedgerCommand(int PayrollRunId) : IRequest<Result<int>>;

public sealed class FreezeLedgerCommandHandler : IRequestHandler<FreezeLedgerCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public FreezeLedgerCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(FreezeLedgerCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsPost))
        {
            return Result<int>.Failure("Forbidden: You do not have permission to post/freeze ledgers.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result<int>.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result<int>.Failure("Forbidden: You do not have access to this company's payroll runs.");
        }

        if (run.Status != PayrollRunStatus.Approved && run.Status != PayrollRunStatus.Posted)
        {
            return Result<int>.Failure($"Cannot freeze ledger: payroll run must be Approved or Posted. Current status is {run.Status}.");
        }

        // Check if ledger already exists for this run (and not reversed)
        var existingLedger = await _unitOfWork.Compliance.GetLedgerHeaderByRunIdAsync(run.Id, cancellationToken);
        if (existingLedger != null && !existingLedger.IsReversed)
        {
            return Result<int>.Failure("Ledger has already been frozen for this payroll run.");
        }

        var activeEmployees = run.Employees.Where(e => !e.IsSuperseded).ToList();
        if (activeEmployees.Count == 0)
        {
            return Result<int>.Failure("Cannot freeze ledger: no active employee calculations found in this run.");
        }

        // 1. Calculate Totals
        decimal totalGross = activeEmployees.Sum(e => e.GrossPay);
        decimal totalPAYE = activeEmployees.Sum(e => e.PayeTax);
        decimal totalFnpfEmployee = activeEmployees.Sum(e => e.FnpfEmployeeContribution);
        decimal totalFnpfEmployer = activeEmployees.Sum(e => e.FnpfEmployerContribution);
        decimal totalNetPay = activeEmployees.Sum(e => e.NetPay);

        // 2. Prepare Ledger Employees to calculate master hash
        var ledgerEmployees = new List<PayrollLedgerEmployee>();
        var employeeHashes = new List<string>();

        foreach (var emp in activeEmployees)
        {
            // Build the string record with 0 as ledger ID
            string recordString = string.Format(CultureInfo.InvariantCulture,
                "ledger:{0}:{1}:{2}:{3}:{4}:{5}:{6}",
                emp.EmployeeId,
                0,
                NormalizeDecimal(emp.GrossPay),
                NormalizeDecimal(emp.PayeTax),
                NormalizeDecimal(emp.FnpfEmployeeContribution),
                NormalizeDecimal(emp.FnpfEmployerContribution),
                NormalizeDecimal(emp.NetPay));

            string empHash = DeterministicHashGenerator.ComputeSha256Hash(recordString);
            employeeHashes.Add(empHash);

            var ledgerEmp = PayrollLedgerEmployee.Create(
                run.CompanyId,
                emp.EmployeeId,
                emp.EmployeeName,
                emp.Tin,
                emp.FnpfNumber,
                emp.GrossPay,
                emp.PayeTax,
                emp.FnpfEmployeeContribution,
                emp.FnpfEmployerContribution,
                emp.NetPay,
                empHash
            );

            // Add components
            foreach (var line in emp.LineItems)
            {
                var comp = PayrollLedgerComponent.Create(
                    line.ComponentCode,
                    line.ComponentName,
                    line.ComponentType,
                    line.Amount
                );
                ledgerEmp.AddComponent(comp);
            }

            ledgerEmployees.Add(ledgerEmp);
        }

        // Compute master hash
        var combinedHashBuilder = new StringBuilder();
        var sortedHashes = employeeHashes.OrderBy(x => x).ToList();
        foreach (var h in sortedHashes)
        {
            combinedHashBuilder.Append(h).Append(';');
        }
        string masterHash = DeterministicHashGenerator.ComputeSha256Hash(combinedHashBuilder.ToString());

        // Create the Ledger Header
        var ledger = PayrollLedger.Create(
            run.CompanyId,
            run.Id,
            totalGross,
            totalPAYE,
            totalFnpfEmployee,
            totalFnpfEmployer,
            totalNetPay,
            _currentUser.Username,
            masterHash
        );

        foreach (var le in ledgerEmployees)
        {
            ledger.AddEmployee(le);
        }

        // 3. Generate double-entry transactions
        // We will generate the detailed journal lines for each employee to maintain the audit trail
        foreach (var emp in activeEmployees)
        {
            // Debit Gross Salary Expense
            ledger.AddTransaction(PayrollLedgerTransaction.Create(
                run.CompanyId,
                null,
                emp.EmployeeId,
                "5000-SAL",
                emp.GrossPay,
                0m,
                $"Gross Salary - {emp.EmployeeName}"
            ));

            // Debit Allowances Expense
            if (emp.TotalAllowances > 0)
            {
                ledger.AddTransaction(PayrollLedgerTransaction.Create(
                    run.CompanyId,
                    null,
                    emp.EmployeeId,
                    "5000-SAL",
                    emp.TotalAllowances,
                    0m,
                    $"Allowances - {emp.EmployeeName}"
                ));
            }

            // Credit Net Payable
            ledger.AddTransaction(PayrollLedgerTransaction.Create(
                run.CompanyId,
                null,
                emp.EmployeeId,
                "2000-PAY",
                0m,
                emp.NetPay,
                $"Net Payable - {emp.EmployeeName}"
            ));

            // Credit PAYE Liability
            if (emp.PayeTax > 0)
            {
                ledger.AddTransaction(PayrollLedgerTransaction.Create(
                    run.CompanyId,
                    null,
                    emp.EmployeeId,
                    "2100-TAX",
                    0m,
                    emp.PayeTax,
                    $"PAYE Liability - {emp.EmployeeName}"
                ));
            }

            // Credit FNPF Employee Portion Liability
            if (emp.FnpfEmployeeContribution > 0)
            {
                ledger.AddTransaction(PayrollLedgerTransaction.Create(
                    run.CompanyId,
                    null,
                    emp.EmployeeId,
                    "2200-FNPF",
                    0m,
                    emp.FnpfEmployeeContribution,
                    $"FNPF EE Contribution - {emp.EmployeeName}"
                ));
            }

            if (emp.FnpfEmployerContribution > 0)
            {
                // Debit FNPF Employer Portion Expense
                ledger.AddTransaction(PayrollLedgerTransaction.Create(
                    run.CompanyId,
                    null,
                    emp.EmployeeId,
                    "5100-FNPF",
                    emp.FnpfEmployerContribution,
                    0m,
                    $"FNPF ER Expense - {emp.EmployeeName}"
                ));

                // Credit FNPF Employer Portion Liability
                ledger.AddTransaction(PayrollLedgerTransaction.Create(
                    run.CompanyId,
                    null,
                    emp.EmployeeId,
                    "2200-FNPF",
                    0m,
                    emp.FnpfEmployerContribution,
                    $"FNPF ER Liability - {emp.EmployeeName}"
                ));
            }

            // Voluntary Deductions (line items with Deduction type except PAYE/FNPF)
            var voluntaryLines = emp.LineItems.Where(l => l.ComponentType == ComponentType.Deduction 
                && !l.ComponentCode.Equals("FNPF_EE", StringComparison.OrdinalIgnoreCase)
                && !l.ComponentCode.Equals("FNPF-EMP", StringComparison.OrdinalIgnoreCase)
                && !l.ComponentCode.Equals(FijiTaxConstants.FnpfEmployeeComponentCode, StringComparison.OrdinalIgnoreCase)
                && !l.ComponentCode.Equals("PAYE", StringComparison.OrdinalIgnoreCase)
                && !l.ComponentCode.Equals(FijiTaxConstants.PayeComponentCode, StringComparison.OrdinalIgnoreCase));

            foreach (var line in voluntaryLines)
            {
                decimal amount = -line.Amount; // Convert negative deduction to positive credit value
                if (amount > 0)
                {
                    ledger.AddTransaction(PayrollLedgerTransaction.Create(
                        run.CompanyId,
                        null,
                        emp.EmployeeId,
                        "2300-VOL",
                        0m,
                        amount,
                        $"Voluntary Deduction ({line.ComponentName}) - {emp.EmployeeName}"
                    ));
                }
            }
        }

        // Validate double-entry balancing
        decimal debits = ledger.Transactions.Sum(t => t.Debit);
        decimal credits = ledger.Transactions.Sum(t => t.Credit);
        if (Math.Abs(debits - credits) > 0.001m)
        {
            return Result<int>.Failure($"Double-entry validation failed: Sum(Debits) [{debits}] must equal Sum(Credits) [{credits}]. Difference: {debits - credits}");
        }

        // Save ledger to database
        await _unitOfWork.Compliance.AddLedgerAsync(ledger, cancellationToken);

        // Transition run status to Posted if it was Approved
        if (run.Status == PayrollRunStatus.Approved)
        {
            run.Post(_currentUser.Username);
            _unitOfWork.PayrollRuns.Update(run);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(ledger.Id);
    }

    private static string NormalizeDecimal(decimal d)
    {
        string formatted = d.ToString("G29", CultureInfo.InvariantCulture);
        if (formatted.Contains('.'))
        {
            formatted = formatted.TrimEnd('0').TrimEnd('.');
        }
        return formatted;
    }
}
