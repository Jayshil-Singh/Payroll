using FijiPayroll.Domain.Entities.Audit;
using CompanyEntity = FijiPayroll.Domain.Entities.Company.Company;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Events;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace FijiPayroll.Domain.Tests.Entities;

/// <summary>
/// Unit tests verifying core business logic, factory methods, and state transitions of the setup domain entities.
/// </summary>
public sealed class SetupDomainTests
{
    [Fact]
    public void Company_ConfigureDetails_UpdatesPropertiesAndValidatesTIN()
    {
        // Arrange
        var company = CompanyEntity.Create("Pacific Supplies Ltd", "KEY_PACIFIC");

        // Act
        company.ConfigureCompanyDetails(
            tradingName: "Pacific Supplies",
            tin: "123456789",
            fnpfNumber: "FNPF-555",
            addr1: "123 Main St",
            addr2: "Suva Port",
            city: "Suva",
            phone: "3334444",
            email: "info@pacific.fj",
            website: "www.pacific.fj"
        );

        // Assert
        company.TradingName.Should().Be("Pacific Supplies");
        company.TIN.Should().Be("123456789");
        company.FnpfEmployerNumber.Should().Be("FNPF-555");
        company.AddressLine1.Should().Be("123 Main St");
        company.AddressLine2.Should().Be("Suva Port");
        company.City.Should().Be("Suva");
        company.Phone.Should().Be("3334444");
        company.Email.Should().Be("info@pacific.fj");
        company.Website.Should().Be("www.pacific.fj");
        company.Country.Should().Be("Fiji");
        company.Locale.Should().Be("en-FJ");
    }

    [Fact]
    public void Company_ConfigureDetails_ThrowsOnInvalidTIN()
    {
        // Arrange
        var company = CompanyEntity.Create("Pacific Supplies Ltd", "KEY_PACIFIC");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => company.ConfigureCompanyDetails(
            tradingName: "Pacific Supplies",
            tin: "123", // Invalid length
            fnpfNumber: "FNPF-555",
            addr1: "123 Main St",
            addr2: "Suva Port",
            city: "Suva",
            phone: "3334444",
            email: "info@pacific.fj",
            website: "www.pacific.fj"
        ));
    }

    [Fact]
    public void Company_MarkSetupCompleted_UpdatesFlagAndSetupDate()
    {
        // Arrange
        var company = CompanyEntity.Create("Pacific Supplies Ltd", "KEY_PACIFIC");

        // Act
        company.MarkSetupCompleted();

        // Assert
        company.IsSetupComplete.Should().BeTrue();
        company.SetupCompletedUtc.Should().NotBeNull();
        company.SetupCompletedUtc.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Company_MarkSetupCompleted_ThrowsIfAlreadyCompleted()
    {
        // Arrange
        var company = CompanyEntity.Create("Pacific Supplies Ltd", "KEY_PACIFIC");
        company.MarkSetupCompleted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => company.MarkSetupCompleted());
    }

    [Fact]
    public void CompanySetupState_Create_BuildsDefaultState()
    {
        // Act
        var state = CompanySetupState.Create(1, "2.0.0");

        // Assert
        state.CompanyId.Should().Be(1);
        state.CurrentStep.Should().Be(WizardStep.Welcome);
        state.IsCompleted.Should().BeFalse();
        state.WizardVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void CompanySetupState_TransitionToStep_UpdatesStepCorrectly()
    {
        // Arrange
        var state = CompanySetupState.Create(1);

        // Act
        state.TransitionToStep(WizardStep.CompanyDetails);

        // Assert
        state.CurrentStep.Should().Be(WizardStep.CompanyDetails);
    }

    [Fact]
    public void CompanySetupState_TransitionToStep_ThrowsIfCompleted()
    {
        // Arrange
        var state = CompanySetupState.Create(1);
        state.CompleteSetup();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => state.TransitionToStep(WizardStep.CompanyDetails));
    }

    [Fact]
    public void CompanySetupTask_Create_PopulatesProperties()
    {
        // Act
        var task = CompanySetupTask.Create(1, 10, WizardStep.CompanyDetails, "admin-user", "1.0.0");

        // Assert
        task.CompanyId.Should().Be(1);
        task.CompanySetupStateId.Should().Be(10);
        task.Step.Should().Be(WizardStep.CompanyDetails);
        task.Completed.Should().BeTrue();
        task.CompletedBy.Should().Be("admin-user");
        task.Version.Should().Be("1.0.0");
        task.CompletedUtc.Should().NotBeNull();
    }

    [Fact]
    public void SetupExecutionRecord_MarkCompleted_UpdatesStatus()
    {
        // Arrange
        var record = SetupExecutionRecord.Create(1, Guid.NewGuid(), "MACH-1", "1.0.0");

        // Act
        record.MarkCompleted();

        // Assert
        record.Status.Should().Be(ExecutionStatus.Completed);
        record.CompletedUtc.Should().NotBeNull();
        record.DurationMilliseconds.Should().NotBeNull();
    }

    [Fact]
    public void SetupExecutionRecord_MarkFailed_UpdatesStatusAndErrors()
    {
        // Arrange
        var record = SetupExecutionRecord.Create(1, Guid.NewGuid(), "MACH-1", "1.0.0");

        // Act
        record.MarkFailed("DB connection timed out", "at Application.DbConnection.Open()");

        // Assert
        record.Status.Should().Be(ExecutionStatus.Failed);
        record.ErrorMessage.Should().Be("DB connection timed out");
        record.ErrorStackTrace.Should().Be("at Application.DbConnection.Open()");
    }

    [Fact]
    public void SetupCheckpoint_MarkCompleted_UpdatesDate()
    {
        // Arrange
        var check = SetupCheckpoint.Create(1, Guid.NewGuid(), WizardStep.CompanyDetails, "Pending", "Started");

        // Act
        check.MarkCompleted("Completed", "Finished successfully");

        // Assert
        check.Status.Should().Be("Completed");
        check.Message.Should().Be("Finished successfully");
        check.CompletedUtc.Should().NotBeNull();
    }

    [Fact]
    public void CompanySetupAudit_Create_PopulatesProperties()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var executionId = Guid.NewGuid();

        // Act
        var audit = CompanySetupAudit.Create(
            companyId: 1,
            step: "CompanyDetails",
            action: "SubmittedDetails",
            result: "TIN verified",
            status: SetupAuditStatus.Success,
            ipAddress: "192.168.1.1",
            machineName: "SERVER-1",
            appVersion: "1.2.0",
            correlationId: correlationId,
            executionId: executionId
        );

        // Assert
        audit.CompanyId.Should().Be(1);
        audit.Step.Should().Be("CompanyDetails");
        audit.Action.Should().Be("SubmittedDetails");
        audit.Result.Should().Be("TIN verified");
        audit.Status.Should().Be(SetupAuditStatus.Success);
        audit.IPAddress.Should().Be("192.168.1.1");
        audit.MachineName.Should().Be("SERVER-1");
        audit.ApplicationVersion.Should().Be("1.2.0");
        audit.CorrelationId.Should().Be(correlationId);
        audit.ExecutionId.Should().Be(executionId);
    }

    [Fact]
    public void FiscalCalendar_Create_PopulatesProperties()
    {
        // Act
        var calendar = FiscalCalendar.Create(
            companyId: 1,
            fiscalYear: 2026,
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 12, 31),
            calendarType: CalendarType.Monthly,
            generatedBy: "admin"
        );

        // Assert
        calendar.CompanyId.Should().Be(1);
        calendar.FiscalYear.Should().Be(2026);
        calendar.StartDate.Should().Be(new DateTime(2026, 1, 1));
        calendar.EndDate.Should().Be(new DateTime(2026, 12, 31));
        calendar.CalendarType.Should().Be(CalendarType.Monthly);
        calendar.GeneratedBy.Should().Be("admin");
        calendar.IsClosed.Should().BeFalse();
        calendar.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void FiscalCalendar_AddPeriod_AppendsPeriod()
    {
        // Arrange
        var calendar = FiscalCalendar.Create(
            companyId: 1,
            fiscalYear: 2026,
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 12, 31),
            calendarType: CalendarType.Monthly,
            generatedBy: "admin"
        );
        var period = FiscalPeriod.Create(1, 12, 1, "Jan 2026", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Act
        calendar.AddPeriod(period);

        // Assert
        calendar.Periods.Should().ContainSingle().Which.Should().Be(period);
    }

    [Fact]
    public void FiscalCalendar_AddPeriod_ThrowsIfLocked()
    {
        // Arrange
        var calendar = FiscalCalendar.Create(
            companyId: 1,
            fiscalYear: 2026,
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 12, 31),
            calendarType: CalendarType.Monthly,
            generatedBy: "admin"
        );
        calendar.LockCalendar();
        var period = FiscalPeriod.Create(1, 12, 1, "Jan 2026", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => calendar.AddPeriod(period));
    }

    [Fact]
    public void CompanySetupCompletedEvent_Create_PopulatesProperties()
    {
        // Arrange
        var executionId = Guid.NewGuid();

        // Act
        var domainEvent = new CompanySetupCompletedEvent(1, executionId, "admin-user");

        // Assert
        domainEvent.CompanyId.Should().Be(1);
        domainEvent.ExecutionId.Should().Be(executionId);
        domainEvent.CompletedBy.Should().Be("admin-user");
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FnpfConfiguration_Create_PopulatesProperties()
    {
        // Act
        var config = FnpfConfiguration.Create(1, 0.10m, 0.08m, new DateTime(2026, 1, 1));

        // Assert
        config.CompanyId.Should().Be(1);
        config.EmployerRate.Should().Be(0.10m);
        config.EmployeeRate.Should().Be(0.08m);
        config.EffectiveDate.Should().Be(new DateTime(2026, 1, 1));
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void FnpfConfiguration_UpdateRates_ModifiesProperties()
    {
        // Arrange
        var config = FnpfConfiguration.Create(1, 0.10m, 0.08m, new DateTime(2026, 1, 1));

        // Act
        config.UpdateRates(0.12m, 0.10m, new DateTime(2026, 6, 1), false);

        // Assert
        config.EmployerRate.Should().Be(0.12m);
        config.EmployeeRate.Should().Be(0.10m);
        config.EffectiveDate.Should().Be(new DateTime(2026, 6, 1));
        config.IsActive.Should().BeFalse();
    }

    [Fact]
    public void BankMaster_Create_PopulatesProperties()
    {
        // Act
        var master = BankMaster.Create(1, "BSP", "Bank South Pacific");

        // Assert
        master.CompanyId.Should().Be(1);
        master.BankCode.Should().Be("BSP");
        master.BankName.Should().Be("Bank South Pacific");
        master.IsActive.Should().BeTrue();
    }

    [Fact]
    public void BankMaster_AddBranch_AddsToCollection()
    {
        // Arrange
        var master = BankMaster.Create(1, "BSP", "Bank South Pacific");
        var branch = BankBranch.Create(1, 1, "SUVA", "Suva Branch", "039-001");

        // Act
        master.AddBranch(branch);

        // Assert
        master.Branches.Should().ContainSingle().Which.Should().Be(branch);
    }

    [Fact]
    public void BankBranch_Create_PopulatesProperties()
    {
        // Act
        var branch = BankBranch.Create(1, 2, "SUVA", "Suva Main", "039-001");

        // Assert
        branch.CompanyId.Should().Be(1);
        branch.BankMasterId.Should().Be(2);
        branch.BranchCode.Should().Be("SUVA");
        branch.BranchName.Should().Be("Suva Main");
        branch.BsbCode.Should().Be("039-001");
        branch.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CompanyBankAccount_Create_PopulatesProperties()
    {
        // Act
        var account = CompanyBankAccount.Create(
            companyId: 1,
            accountName: "Operating Account",
            bankMasterId: 2,
            bankBranchId: 3,
            accountType: BankAccountType.Operating,
            encryptedAccountNumber: "AES256:v1:keyid:cipher",
            accountNumberHash: "hash-12345",
            last4Digits: "9876"
        );

        // Assert
        account.CompanyId.Should().Be(1);
        account.AccountName.Should().Be("Operating Account");
        account.BankMasterId.Should().Be(2);
        account.BankBranchId.Should().Be(3);
        account.AccountType.Should().Be(BankAccountType.Operating);
        account.EncryptedAccountNumber.Should().Be("AES256:v1:keyid:cipher");
        account.AccountNumberHash.Should().Be("hash-12345");
        account.Last4Digits.Should().Be("9876");
        account.GetMaskedAccountNumber().Should().Be("******9876");
    }

    [Fact]
    public void ApprovalConfig_Create_PopulatesProperties()
    {
        // Act
        var config = ApprovalConfig.Create(1, "user-guid-123", null, 1, ApprovalRole.FinanceManager);

        // Assert
        config.CompanyId.Should().Be(1);
        config.UserId.Should().Be("user-guid-123");
        config.EmployeeId.Should().BeNull();
        config.ApprovalLevel.Should().Be(1);
        config.Role.Should().Be(ApprovalRole.FinanceManager);
    }

    [Fact]
    public void ApprovalConfig_Create_ThrowsIfBothNullOrBothProvided()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApprovalConfig.Create(1, null, null, 1, ApprovalRole.FinanceManager));
        Assert.Throws<ArgumentException>(() => ApprovalConfig.Create(1, "user-id", 5, 1, ApprovalRole.FinanceManager));
    }
}
