using System;
using System.Collections.Generic;
using System.Text;
using FijiPayroll.SDK.Contracts;
using FijiPayroll.SDK.Interfaces;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Infrastructure service for generating FRCS MER and FNPF Contribution CSV file exports.
/// </summary>
public sealed class ComplianceFileService : IComplianceFileService
{
    /// <inheritdoc/>
    public string GenerateFrcsCsv(string employerTin, IEnumerable<PaymentDetail> payments, int month, int year)
    {
        if (string.IsNullOrWhiteSpace(employerTin)) throw new ArgumentException("Employer TIN is required.", nameof(employerTin));
        if (payments == null) throw new ArgumentNullException(nameof(payments));

        var sb = new StringBuilder();
        // Header line (standard for MER)
        sb.AppendLine("EmployerTIN,Month,Year,EmployeeTIN,EmployeeName,GrossPay,PAYEDeducted");

        foreach (var p in payments)
        {
            string safeName = EscapeCsvValue(p.EmployeeName);
            sb.AppendLine($"{employerTin},{month:D2},{year},{p.Tin},{safeName},{p.Gross:F2},{p.Paye:F2}");
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string GenerateFnpfCsv(string employerNumber, string employerName, int month, int year, IEnumerable<PaymentDetail> payments)
    {
        if (string.IsNullOrWhiteSpace(employerNumber)) throw new ArgumentException("Employer number is required.", nameof(employerNumber));
        if (payments == null) throw new ArgumentNullException(nameof(payments));

        var sb = new StringBuilder();
        // Header line for FNPF Portal uploads
        sb.AppendLine("EmployerNumber,EmployerName,Month,Year,FNPFNumber,EmployeeName,EmployeeContribution,EmployerContribution");

        string safeEmployerName = EscapeCsvValue(employerName);
        foreach (var p in payments)
        {
            string safeName = EscapeCsvValue(p.EmployeeName);
            sb.AppendLine($"{employerNumber},{safeEmployerName},{month:D2},{year},{p.FnpfNumber},{safeName},{p.FnpfEmployee:F2},{p.FnpfEmployer:F2}");
        }

        return sb.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
