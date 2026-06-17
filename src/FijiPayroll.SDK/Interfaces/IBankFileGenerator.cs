using System;
using System.Collections.Generic;
using FijiPayroll.SDK.Contracts;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Defines the plugin extension boundary for implementing direct-credit clearing file layouts in Fiji.
/// </summary>
public interface IBankFileGenerator
{
    /// <summary>Gets the unique bank code (e.g. "BSP", "ANZ", "WBC").</summary>
    string BankCode { get; }

    /// <summary>Gets the user-friendly bank name.</summary>
    string BankName { get; }

    /// <summary>
    /// Formats the direct-credit dataset into a structured file output.
    /// </summary>
    /// <param name="companyName">The corporate payer name.</param>
    /// <param name="companyAccount">The employer's credit source account number.</param>
    /// <param name="bsb">The employer's bank BSB code (optional/required by bank).</param>
    /// <param name="paymentDate">The processing date.</param>
    /// <param name="reference">Payment description code.</param>
    /// <param name="payments">The sequence of employee payment details.</param>
    /// <param name="headerTemplate">Format string template for the file header.</param>
    /// <param name="detailTemplate">Format string template for each detail row.</param>
    /// <param name="footerTemplate">Format string template for the file trailer.</param>
    /// <param name="delimiter">The column delimiter character.</param>
    /// <returns>Formated text file content block.</returns>
    string Generate(
        string companyName,
        string companyAccount,
        string bsb,
        DateTime paymentDate,
        string reference,
        IEnumerable<PaymentDetail> payments,
        string headerTemplate,
        string detailTemplate,
        string footerTemplate,
        char delimiter);
}
