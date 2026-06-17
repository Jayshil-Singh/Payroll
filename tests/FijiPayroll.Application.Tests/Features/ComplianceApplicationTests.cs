using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.SDK.Contracts;
using FijiPayroll.SDK.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features;

public sealed class ComplianceApplicationTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RuleSimulationEngine> _simulationLogger;
    private readonly ILogger<ComplianceValidationService> _validationLogger;

    public ComplianceApplicationTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _simulationLogger = Substitute.For<ILogger<RuleSimulationEngine>>();
        _validationLogger = Substitute.For<ILogger<ComplianceValidationService>>();
    }

    [Fact]
    public async Task SimulateRuleChangeAsync_ShouldCalculateVariances_Correctly()
    {
        // Arrange
        var ledger1 = PayrollLedger.Create(
            companyId: 1,
            payrollRunId: 100,
            employeeId: 10,
            employeeName: "John Doe",
            employeeTin: "999888777",
            employeeFnpfNumber: "12345",
            gross: 1000m,
            paye: 100m,
            fnpfEmployee: 80m,  // 8% of 1000
            fnpfEmployer: 100m, // 10% of 1000
            netPay: 820m,
            createdBy: "admin",
            hash: "mock-ledger-hash"
        );

        var ledgersList = new List<PayrollLedger> { ledger1 };
        _unitOfWork.Compliance.GetLedgerByRunIdAsync(100, Arg.Any<CancellationToken>())
            .Returns(ledgersList);

        // Define rule overrides (change FNPF employee rate to 10% and employer rate to 12% in simulation)
        var overrides = new List<RuleSimulationEngine.RuleOverride>
        {
            new("FNPF_EE_RATE", "0.10"),
            new("FNPF_ER_RATE", "0.12"),
            new("PAYE_TAX_FREE_THRESHOLD", "20000"), // Annual taxable threshold
            new("PAYE_BRACKET_1_RATE", "0.18"),
            new("PAYE_BRACKET_2_RATE", "0.20")
        };

        var engine = new RuleSimulationEngine(_unitOfWork, _simulationLogger);

        // Act
        var result = await engine.SimulateRuleChangeAsync(1, 100, overrides, CancellationToken.None);

        // Assert
        result.OriginalTotalFnpf.Should().Be(180m); // 80 + 100
        // Simulated: 1000 * 10% (employee) + 1000 * 12% (employer) = 100 + 120 = 220
        result.SimulatedTotalFnpf.Should().Be(220m);
        result.FnpfVariance.Should().Be(40m); // 220 - 180

        result.EmployeeVariances.Should().ContainSingle();
        var variance = result.EmployeeVariances[0];
        variance.EmployeeId.Should().Be(10);
        variance.EmployeeName.Should().Be("John Doe");
        variance.OriginalFnpf.Should().Be(180m);
        variance.SimulatedFnpf.Should().Be(220m);
        variance.FnpfDifference.Should().Be(40m);
    }

    [Fact]
    public void ComplianceValidationService_ValidateDataset_ReturnsTinErrors_WhenTinIsInvalidOrMissing()
    {
        // Arrange
        var validator = Substitute.For<IComplianceValidator>();
        var validators = new List<IComplianceValidator> { validator };

        var paymentDetails = new List<PaymentDetail>
        {
            // Missing TIN
            new PaymentDetail(
                EmployeeId: 1,
                EmployeeName: "Alice Smith",
                Tin: "",
                FnpfNumber: "1111",
                Gross: 500m,
                Paye: 50m,
                FnpfEmployee: 40m,
                FnpfEmployer: 50m,
                BankAccountNumber: "123456",
                Amount: 410m
            ),
            // Invalid TIN (too short)
            new PaymentDetail(
                EmployeeId: 2,
                EmployeeName: "Bob Jones",
                Tin: "12345",
                FnpfNumber: "2222",
                Gross: 600m,
                Paye: 60m,
                FnpfEmployee: 48m,
                FnpfEmployer: 60m,
                BankAccountNumber: "654321",
                Amount: 492m
            )
        };

        var service = new ComplianceValidationService(validators, _validationLogger);

        // Act
        var issues = service.ValidateDataset(1, paymentDetails).ToList();

        // Assert
        issues.Count(x => x.RuleCode == "FRCS_TIN_MISSING").Should().Be(1);
        issues.Count(x => x.RuleCode == "FRCS_TIN_INVALID").Should().Be(1);

        var missingTinIssue = issues.First(x => x.RuleCode == "FRCS_TIN_MISSING");
        missingTinIssue.AffectedEmployee.Should().Be("Alice Smith");
        missingTinIssue.Severity.Should().Be("Error");

        var invalidTinIssue = issues.First(x => x.RuleCode == "FRCS_TIN_INVALID");
        invalidTinIssue.AffectedEmployee.Should().Be("Bob Jones");
        invalidTinIssue.Severity.Should().Be("Error");
    }

    [Fact]
    public void ComplianceValidationService_ValidateDataset_ReturnsFnpfErrors_WhenFnpfNumberIsMissingForDeduction()
    {
        // Arrange
        var validator = Substitute.For<IComplianceValidator>();
        var validators = new List<IComplianceValidator> { validator };

        var paymentDetails = new List<PaymentDetail>
        {
            // Missing FNPF number but deduction exists (FnpfEmployee > 0)
            new PaymentDetail(
                EmployeeId: 3,
                EmployeeName: "Charlie Brown",
                Tin: "123456789",
                FnpfNumber: "",
                Gross: 500m,
                Paye: 50m,
                FnpfEmployee: 40m,
                FnpfEmployer: 50m,
                BankAccountNumber: "123456",
                Amount: 410m
            )
        };

        var service = new ComplianceValidationService(validators, _validationLogger);

        // Act
        var issues = service.ValidateDataset(1, paymentDetails).ToList();

        // Assert
        issues.Should().ContainSingle();
        var issue = issues[0];
        issue.RuleCode.Should().Be("FNPF_NUM_MISSING");
        issue.AffectedEmployee.Should().Be("Charlie Brown");
        issue.Severity.Should().Be("Error");
    }

    [Fact]
    public void ComplianceValidationService_ValidateDataset_ReturnsBankAccountWarnings_WhenBankAccountIsMissing()
    {
        // Arrange
        var validator = Substitute.For<IComplianceValidator>();
        var validators = new List<IComplianceValidator> { validator };

        var paymentDetails = new List<PaymentDetail>
        {
            // Missing Bank Account
            new PaymentDetail(
                EmployeeId: 4,
                EmployeeName: "David Lee",
                Tin: "123456789",
                FnpfNumber: "44444",
                Gross: 500m,
                Paye: 50m,
                FnpfEmployee: 40m,
                FnpfEmployer: 50m,
                BankAccountNumber: "",
                Amount: 410m
            )
        };

        var service = new ComplianceValidationService(validators, _validationLogger);

        // Act
        var issues = service.ValidateDataset(1, paymentDetails).ToList();

        // Assert
        issues.Should().ContainSingle();
        var issue = issues[0];
        issue.RuleCode.Should().Be("BANK_ACCT_MISSING");
        issue.AffectedEmployee.Should().Be("David Lee");
        issue.Severity.Should().Be("Warning");
    }
}
