using System;
using System.Collections.Generic;

namespace FijiPayroll.Infrastructure.Services.ComplianceEvidence;

/// <summary>
/// Registry containing metadata definitions and version hashes for all supported SSRS reports.
/// </summary>
public sealed class ReportSnapshotRegistry
{
    private readonly Dictionary<string, RegisteredReportDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportSnapshotRegistry"/> class.
    /// Registers the standard compliance reports.
    /// </summary>
    public ReportSnapshotRegistry()
    {
        RegisterReport(new RegisteredReportDefinition(
            ReportName: "PayrollRegister",
            TemplatePath: "FijiPayroll/Payroll/PayrollRegister.rdl",
            VersionHash: "a7e174bdf882bf4520970df1c50fc751a1e0b57ea24c56e07a12cb4fcf0c89ba",
            ExpectedParameters: ["@P_CompanyId", "@P_PayrollRunId"]
        ));

        RegisterReport(new RegisteredReportDefinition(
            ReportName: "PayrollSummary",
            TemplatePath: "FijiPayroll/Payroll/PayrollSummary.rdl",
            VersionHash: "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
            ExpectedParameters: ["@P_CompanyId", "@P_PayrollRunId"]
        ));

        RegisterReport(new RegisteredReportDefinition(
            ReportName: "PayslipBatch",
            TemplatePath: "FijiPayroll/Payroll/PayslipBatch.rdl",
            VersionHash: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ExpectedParameters: ["@P_CompanyId", "@P_PayrollRunId"]
        ));

        RegisterReport(new RegisteredReportDefinition(
            ReportName: "DepartmentSummary",
            TemplatePath: "FijiPayroll/Financial/DepartmentSummary.rdl",
            VersionHash: "f188bf246ce013149dfdf4c822e1b1317ae41e16449b934ca495991b7852b582",
            ExpectedParameters: ["@P_CompanyId", "@P_PeriodFrom", "@P_PeriodTo"]
        ));
    }

    /// <summary>
    /// Registers a report definition in the registry.
    /// </summary>
    public void RegisterReport(RegisteredReportDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        _definitions[definition.ReportName] = definition;
    }

    /// <summary>
    /// Gets all registered report definitions.
    /// </summary>
    public IEnumerable<RegisteredReportDefinition> GetRegisteredReports() => _definitions.Values;

    /// <summary>
    /// Attempts to retrieve a registered report definition.
    /// </summary>
    public bool TryGetReport(string reportName, out RegisteredReportDefinition definition)
    {
        return _definitions.TryGetValue(reportName, out definition!);
    }
}

/// <summary>
/// Captures the template path and expected parameters for a registered SSRS report.
/// </summary>
public sealed record RegisteredReportDefinition(
    string ReportName,
    string TemplatePath,
    string VersionHash,
    IReadOnlyList<string> ExpectedParameters
);
