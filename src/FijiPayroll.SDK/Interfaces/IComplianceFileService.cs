using System.Collections.Generic;
using FijiPayroll.SDK.Contracts;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Interface governing the core compliance formatting operations for Fiji Revenue & Customs Service and Fiji National Provident Fund returns.
/// </summary>
public interface IComplianceFileService
{
    /// <summary>
    /// Builds the comma-separated data lines for the FRCS Monthly Employer Return (MER).
    /// </summary>
    /// <param name="employerTin">9-digit employer Tax Identification Number.</param>
    /// <param name="payments">Sequence of employee payment records containing gross, allowances, and PAYE.</param>
    /// <param name="month">1-indexed period month (MM).</param>
    /// <param name="year">Four-digit period year (YYYY).</param>
    /// <returns>UTF-8 formatted comma-separated content block without headers.</returns>
    string GenerateFrcsCsv(
        string employerTin,
        IEnumerable<PaymentDetail> payments,
        int month,
        int year);

    /// <summary>
    /// Builds the comma-separated contribution columns for the FNPF remittance portal.
    /// </summary>
    /// <param name="employerNumber">FNPF employer registration number.</param>
    /// <param name="employerName">FNPF company name.</param>
    /// <param name="month">1-indexed period month.</param>
    /// <param name="year">Four-digit period year.</param>
    /// <param name="payments">Sequence of employee payment records containing employee/employer contribution amounts.</param>
    /// <returns>UTF-8 formatted CSV block with FNPF header columns.</returns>
    string GenerateFnpfCsv(
        string employerNumber,
        string employerName,
        int month,
        int year,
        IEnumerable<PaymentDetail> payments);
}
