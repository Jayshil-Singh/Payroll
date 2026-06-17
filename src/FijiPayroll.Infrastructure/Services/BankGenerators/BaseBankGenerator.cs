using System;
using System.Collections.Generic;
using System.Text;
using FijiPayroll.SDK.Contracts;
using FijiPayroll.SDK.Interfaces;

namespace FijiPayroll.Infrastructure.Services.BankGenerators;

/// <summary>
/// Abstract base class implementing IBankFileGenerator, providing placeholder token replacement utilities.
/// </summary>
public abstract class BaseBankGenerator : IBankFileGenerator
{
    /// <inheritdoc/>
    public abstract string BankCode { get; }

    /// <inheritdoc/>
    public abstract string BankName { get; }

    /// <inheritdoc/>
    public string Generate(
        string companyName,
        string companyAccount,
        string bsb,
        DateTime paymentDate,
        string reference,
        IEnumerable<PaymentDetail> payments,
        string headerTemplate,
        string detailTemplate,
        string footerTemplate,
        char delimiter)
    {
        var paymentList = new List<PaymentDetail>(payments);
        decimal totalAmount = 0;
        foreach (var p in paymentList) totalAmount += p.Amount;
        int totalCount = paymentList.Count;

        var sb = new StringBuilder();

        // 1. Process Header
        if (!string.IsNullOrWhiteSpace(headerTemplate))
        {
            var headerTokens = new Dictionary<string, object>
            {
                { "CompanyName", companyName },
                { "CompanyAccount", companyAccount },
                { "Bsb", bsb },
                { "PaymentDate", paymentDate },
                { "Reference", reference },
                { "TotalAmount", totalAmount },
                { "TotalCount", totalCount }
            };
            sb.AppendLine(ReplaceTokens(headerTemplate, headerTokens));
        }

        // 2. Process Details
        foreach (var p in paymentList)
        {
            if (!string.IsNullOrWhiteSpace(detailTemplate))
            {
                var detailTokens = new Dictionary<string, object>
                {
                    { "EmployeeName", p.EmployeeName },
                    { "BankAccountNumber", p.BankAccountNumber },
                    { "Amount", p.Amount },
                    { "EmployeeId", p.EmployeeId.ToString() },
                    { "Reference", reference }
                };
                sb.AppendLine(ReplaceTokens(detailTemplate, detailTokens));
            }
        }

        // 3. Process Footer
        if (!string.IsNullOrWhiteSpace(footerTemplate))
        {
            var footerTokens = new Dictionary<string, object>
            {
                { "TotalAmount", totalAmount },
                { "TotalCount", totalCount }
            };
            sb.AppendLine(ReplaceTokens(footerTemplate, footerTokens));
        }

        return sb.ToString().TrimEnd();
    }

    private static string ReplaceTokens(string template, Dictionary<string, object> tokens)
    {
        string result = template;
        foreach (var kvp in tokens)
        {
            string key = "{" + kvp.Key + "}";
            if (result.Contains(key))
            {
                result = result.Replace(key, kvp.Value?.ToString() ?? "");
            }

            // Parse formatted tokens like {PaymentDate:yyyyMMdd} or {Amount:F2}
            string prefix = "{" + kvp.Key + ":";
            int idx;
            while ((idx = result.IndexOf(prefix)) != -1)
            {
                int endIdx = result.IndexOf("}", idx);
                if (endIdx != -1)
                {
                    string format = result.Substring(idx + prefix.Length, endIdx - (idx + prefix.Length));
                    string replacement = string.Empty;

                    if (kvp.Value is DateTime dt)
                    {
                        replacement = dt.ToString(format);
                    }
                    else if (kvp.Value is decimal dec)
                    {
                        replacement = dec.ToString(format);
                    }
                    else
                    {
                        replacement = kvp.Value?.ToString() ?? "";
                    }

                    result = result.Remove(idx, endIdx - idx + 1).Insert(idx, replacement);
                }
                else
                {
                    break;
                }
            }
        }
        return result;
    }
}
