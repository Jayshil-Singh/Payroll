using System;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing a Fiji Revenue & Customs Service (FRCS) Monthly Employer Return (MER) filing submission.
/// Stores pinned version hashes to ensure reproducible audits.
/// </summary>
public sealed class FRCSSubmission : AuditableEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the target compliance period ID.</summary>
    public int CompliancePeriodId { get; private set; }

    /// <summary>Gets the current filing status of the return.</summary>
    public ComplianceSubmissionStatus Status { get; private set; }

    /// <summary>Gets the raw text CSV content representing the MER data payload.</summary>
    public string FrcsFileContent { get; private set; } = string.Empty;

    /// <summary>Gets the output file path on the system filesystem.</summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>Gets the integrity hash validating the return file content.</summary>
    public string Hash { get; private set; } = string.Empty;

    /// <summary>Gets the platform calculation engine version pinned at generation time.</summary>
    public string CalculationEngineVersion { get; private set; } = string.Empty;

    /// <summary>Gets the formula calculation version pinned at generation time.</summary>
    public string FormulaEngineVersion { get; private set; } = string.Empty;

    /// <summary>Gets the compliance module version pinned at generation time.</summary>
    public string ComplianceEngineVersion { get; private set; } = string.Empty;

    /// <summary>Gets the statutory rules version pinned at generation time.</summary>
    public string PinnedRuleVersion { get; private set; } = string.Empty;

    private FRCSSubmission() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new FRCSSubmission.
    /// </summary>
    public static FRCSSubmission Create(
        int companyId,
        int compliancePeriodId,
        string frcsFileContent,
        string filePath,
        string hash,
        string calcEngineVer,
        string formulaEngineVer,
        string compEngineVer,
        string pinnedRuleVer)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (compliancePeriodId <= 0) throw new ArgumentOutOfRangeException(nameof(compliancePeriodId));
        if (string.IsNullOrWhiteSpace(frcsFileContent)) throw new ArgumentException("File content cannot be empty.", nameof(frcsFileContent));
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Hash cannot be empty.", nameof(hash));

        return new FRCSSubmission
        {
            CompanyId = companyId,
            CompliancePeriodId = compliancePeriodId,
            FrcsFileContent = frcsFileContent,
            FilePath = filePath,
            Hash = hash,
            CalculationEngineVersion = calcEngineVer,
            FormulaEngineVersion = formulaEngineVer,
            ComplianceEngineVersion = compEngineVer,
            PinnedRuleVersion = pinnedRuleVer,
            Status = ComplianceSubmissionStatus.Draft
        };
    }

    /// <summary>Updates the submission status.</summary>
    public void UpdateStatus(ComplianceSubmissionStatus status)
    {
        Status = status;
    }
}
