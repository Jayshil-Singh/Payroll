using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FijiPayroll.Domain.Entities.Payroll;

namespace FijiPayroll.Application.Services.EvidencePack;

/// <summary>
/// Verifies the integrity of the immutable PayrollLedger records by re-computing and validating their SHA-256 hashes.
/// </summary>
public sealed class LedgerIntegrityVerifier
{
    /// <summary>
    /// Computes the master hash for a dataset of ledgers and validates that all individual record hashes match.
    /// </summary>
    /// <param name="ledgers">The ledger records to verify.</param>
    /// <returns>A tuple containing the master hash, the record count, and the overall verification status (PASS or FAIL).</returns>
    public (string MasterHash, int RecordCount, string IntegrityStatus) VerifyLedgerIntegrity(
        IEnumerable<PayrollLedger> ledgers)
    {
        if (ledgers == null)
        {
            return (string.Empty, 0, "FAIL");
        }

        var ledgerList = ledgers.ToList();
        if (ledgerList.Count == 0)
        {
            return (DeterministicHashGenerator.ComputeSha256Hash("empty-ledger"), 0, "PASS");
        }

        // Enforce strict deterministic sorting by EmployeeId to prevent order-dependency
        var sortedLedgers = ledgerList.OrderBy(x => x.EmployeeId).ToList();

        var combinedHashBuilder = new StringBuilder();
        bool anyRecordTampered = false;

        foreach (var ledger in sortedLedgers)
        {
            // Compute the deterministic string representation of this ledger record
            string recordString = FormatLedgerRecord(ledger);
            string calculatedHash = DeterministicHashGenerator.ComputeSha256Hash(recordString);

            // If the calculated hash does not match the stored Hash property, flag it as tampered.
            // Note: If ledger.Hash is missing or empty, it counts as a failure.
            if (string.IsNullOrWhiteSpace(ledger.Hash) || 
                !string.Equals(ledger.Hash, calculatedHash, StringComparison.OrdinalIgnoreCase))
            {
                anyRecordTampered = true;
            }

            combinedHashBuilder.Append(calculatedHash).Append(';');
        }

        string masterHash = DeterministicHashGenerator.ComputeSha256Hash(combinedHashBuilder.ToString());
        string integrityStatus = anyRecordTampered ? "FAIL" : "PASS";

        return (masterHash, sortedLedgers.Count, integrityStatus);
    }

    /// <summary>
    /// Formats a single ledger record deterministically for hashing.
    /// </summary>
    public static string FormatLedgerRecord(PayrollLedger r)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "ledger:{0}:{1}:{2}:{3}:{4}:{5}:{6}",
            r.EmployeeId,
            r.PayrollRunId,
            NormalizeDecimal(r.Gross),
            NormalizeDecimal(r.PAYE),
            NormalizeDecimal(r.FNPFEmployee),
            NormalizeDecimal(r.FNPFEmployer),
            NormalizeDecimal(r.NetPay));
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
