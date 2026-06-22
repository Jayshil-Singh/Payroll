using System;
using FijiPayroll.Domain.Entities.Payroll;
using FluentAssertions;
using Xunit;

namespace FijiPayroll.Domain.Tests.Entities;

/// <summary>
/// Unit tests verifying core business logic, factory methods, and validation rules for the compliance domain entities.
/// </summary>
public sealed class ComplianceDomainTests
{
    [Fact]
    public void ComplianceSnapshot_Create_PopulatesPropertiesCorrectly()
    {
        // Act
        var snapshot = ComplianceSnapshot.Create(
            companyId: 1,
            complianceBatchId: 10,
            payrollRunId: 5,
            snapshotVersion: "v1.0.0",
            snapshotJson: "{ \"EmployeeCount\": 20 }",
            sha256Hash: "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
            createdBy: "system-admin"
        );

        // Assert
        snapshot.CompanyId.Should().Be(1);
        snapshot.ComplianceBatchId.Should().Be(10);
        snapshot.PayrollRunId.Should().Be(5);
        snapshot.SnapshotVersion.Should().Be("v1.0.0");
        snapshot.SnapshotJson.Should().Be("{ \"EmployeeCount\": 20 }");
        snapshot.SHA256Hash.Should().Be("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
        snapshot.CreatedBy.Should().Be("system-admin");
        snapshot.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ComplianceSnapshot_Create_ThrowsExceptions_OnInvalidParameters()
    {
        // Assert & Act
        Assert.Throws<ArgumentOutOfRangeException>(() => ComplianceSnapshot.Create(0, 1, 1, "v1", "{}", "hash", "user"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ComplianceSnapshot.Create(1, 1, 0, "v1", "{}", "hash", "user"));
        Assert.Throws<ArgumentException>(() => ComplianceSnapshot.Create(1, 1, 1, "", "{}", "hash", "user"));
        Assert.Throws<ArgumentException>(() => ComplianceSnapshot.Create(1, 1, 1, "v1", "", "hash", "user"));
        Assert.Throws<ArgumentException>(() => ComplianceSnapshot.Create(1, 1, 1, "v1", "{}", "", "user"));
    }

    [Fact]
    public void ComplianceAmendment_Create_PopulatesPropertiesCorrectly()
    {
        // Act
        var amendment = ComplianceAmendment.Create(
            companyId: 1,
            originalSubmissionId: 1,
            previousSubmissionId: 2,
            currentSubmissionId: 3,
            reason: "Underreported gross pay correction",
            createdBy: "compliance-officer"
        );

        // Assert
        amendment.CompanyId.Should().Be(1);
        amendment.OriginalSubmissionId.Should().Be(1);
        amendment.PreviousSubmissionId.Should().Be(2);
        amendment.CurrentSubmissionId.Should().Be(3);
        amendment.Reason.Should().Be("Underreported gross pay correction");
        amendment.CreatedBy.Should().Be("compliance-officer");
        amendment.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ComplianceAmendment_Create_ThrowsExceptions_OnInvalidParameters()
    {
        // Assert & Act
        Assert.Throws<ArgumentOutOfRangeException>(() => ComplianceAmendment.Create(0, 1, 2, 3, "reason", "user"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ComplianceAmendment.Create(1, 0, 2, 3, "reason", "user"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ComplianceAmendment.Create(1, 1, 0, 3, "reason", "user"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ComplianceAmendment.Create(1, 1, 2, 0, "reason", "user"));
        Assert.Throws<ArgumentException>(() => ComplianceAmendment.Create(1, 1, 2, 3, "", "user"));
    }

    [Fact]
    public void StatutoryRule_Create_PopulatesPropertiesCorrectly()
    {
        // Act
        var rule = StatutoryRule.Create(
            authority: "FNPF",
            ruleCode: "FNPF_EE_RATE",
            ruleValue: "0.08",
            description: "Employee FNPF remittance rate setting",
            effectiveFrom: new DateTime(2026, 1, 1),
            effectiveTo: new DateTime(2026, 12, 31)
        );

        // Assert
        rule.Authority.Should().Be("FNPF");
        rule.RuleCode.Should().Be("FNPF_EE_RATE");
        rule.RuleValue.Should().Be("0.08");
        rule.Description.Should().Be("Employee FNPF remittance rate setting");
        rule.EffectiveFrom.Should().Be(new DateTime(2026, 1, 1));
        rule.EffectiveTo.Should().Be(new DateTime(2026, 12, 31));
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void StatutoryRule_Create_ThrowsException_OnInvalidEffectiveDates()
    {
        // Assert & Act
        Assert.Throws<ArgumentException>(() => StatutoryRule.Create(
            "FNPF",
            "FNPF_EE_RATE",
            "0.08",
            "description",
            new DateTime(2026, 12, 31),
            new DateTime(2026, 1, 1) // Effective to before effective from
        ));
    }

    [Fact]
    public void StatutoryRule_ToggleActivation_ModifiesIsActiveState()
    {
        // Arrange
        var rule = StatutoryRule.Create("FRCS", "PAYE_THRESHOLD", "30000", "threshold", new DateTime(2026, 1, 1));

        // Act
        rule.Deactivate();
        // Assert
        rule.IsActive.Should().BeFalse();

        // Act
        rule.Activate();
        // Assert
        rule.IsActive.Should().BeTrue();
    }
}
