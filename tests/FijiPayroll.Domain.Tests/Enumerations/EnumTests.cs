using FijiPayroll.Domain.Enumerations;
using FluentAssertions;
using System;
using Xunit;

namespace FijiPayroll.Domain.Tests.Enumerations;

/// <summary>
/// Unit tests verifying value mapping, type casting, and string conversions of all Phase 10 enums.
/// </summary>
public sealed class EnumTests
{
    [Theory]
    [InlineData(WizardStep.Welcome, 1)]
    [InlineData(WizardStep.CompanyDetails, 2)]
    [InlineData(WizardStep.FiscalCalendar, 3)]
    [InlineData(WizardStep.PayrollFrequency, 4)]
    [InlineData(WizardStep.BankConfiguration, 5)]
    [InlineData(WizardStep.Approvers, 6)]
    [InlineData(WizardStep.Validation, 7)]
    [InlineData(WizardStep.Completed, 8)]
    public void WizardStep_Should_CastToExpectedInteger(WizardStep step, int expectedVal)
    {
        ((int)step).Should().Be(expectedVal);
    }

    [Theory]
    [InlineData(BankAccountType.Operating, 1)]
    [InlineData(BankAccountType.Payroll, 2)]
    [InlineData(BankAccountType.Tax, 3)]
    [InlineData(BankAccountType.FNPF, 4)]
    [InlineData(BankAccountType.Savings, 5)]
    [InlineData(BankAccountType.Trust, 6)]
    [InlineData(BankAccountType.Custom, 7)]
    public void BankAccountType_Should_CastToExpectedInteger(BankAccountType type, int expectedVal)
    {
        ((int)type).Should().Be(expectedVal);
    }

    [Theory]
    [InlineData(CalendarType.Weekly, 1)]
    [InlineData(CalendarType.Fortnightly, 2)]
    [InlineData(CalendarType.Monthly, 3)]
    [InlineData(CalendarType.Custom, 4)]
    public void CalendarType_Should_CastToExpectedInteger(CalendarType type, int expectedVal)
    {
        ((int)type).Should().Be(expectedVal);
    }

    [Theory]
    [InlineData(FrequencyCode.Weekly, 1)]
    [InlineData(FrequencyCode.Fortnightly, 2)]
    [InlineData(FrequencyCode.BiMonthly, 3)]
    [InlineData(FrequencyCode.Monthly, 4)]
    [InlineData(FrequencyCode.Custom, 5)]
    public void FrequencyCode_Should_CastToExpectedInteger(FrequencyCode code, int expectedVal)
    {
        ((int)code).Should().Be(expectedVal);
    }

    [Theory]
    [InlineData(ApprovalRole.PayrollOfficer, 1)]
    [InlineData(ApprovalRole.PayrollSupervisor, 2)]
    [InlineData(ApprovalRole.FinanceManager, 3)]
    [InlineData(ApprovalRole.HRManager, 4)]
    [InlineData(ApprovalRole.Administrator, 5)]
    public void ApprovalRole_Should_CastToExpectedInteger(ApprovalRole role, int expectedVal)
    {
        ((int)role).Should().Be(expectedVal);
    }

    [Theory]
    [InlineData(ExecutionStatus.Pending, 1)]
    [InlineData(ExecutionStatus.Running, 2)]
    [InlineData(ExecutionStatus.Completed, 3)]
    [InlineData(ExecutionStatus.Failed, 4)]
    [InlineData(ExecutionStatus.RolledBack, 5)]
    [InlineData(ExecutionStatus.Retrying, 6)]
    [InlineData(ExecutionStatus.Cancelled, 7)]
    public void ExecutionStatus_Should_CastToExpectedInteger(ExecutionStatus status, int expectedVal)
    {
        ((int)status).Should().Be(expectedVal);
    }

    [Theory]
    [InlineData(SetupAuditStatus.Success, 1)]
    [InlineData(SetupAuditStatus.Warning, 2)]
    [InlineData(SetupAuditStatus.Failed, 3)]
    public void SetupAuditStatus_Should_CastToExpectedInteger(SetupAuditStatus status, int expectedVal)
    {
        ((int)status).Should().Be(expectedVal);
    }

    [Theory]
    [InlineData(SeedCategory.Banks, 1)]
    [InlineData(SeedCategory.Branches, 2)]
    [InlineData(SeedCategory.LeaveTypes, 3)]
    [InlineData(SeedCategory.PayrollComponents, 4)]
    [InlineData(SeedCategory.Reports, 5)]
    [InlineData(SeedCategory.Roles, 6)]
    [InlineData(SeedCategory.Permissions, 7)]
    [InlineData(SeedCategory.Settings, 8)]
    public void SeedCategory_Should_CastToExpectedInteger(SeedCategory category, int expectedVal)
    {
        ((int)category).Should().Be(expectedVal);
    }
}
