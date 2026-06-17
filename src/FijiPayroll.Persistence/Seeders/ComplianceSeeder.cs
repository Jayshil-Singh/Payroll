using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FijiPayroll.Persistence.Seeders;

/// <summary>
/// Seeds standard statutory rules and bank clearing layout templates.
/// </summary>
public sealed class ComplianceSeeder
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceSeeder"/> class.
    /// </summary>
    public ComplianceSeeder(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Seeds the default compliance records asynchronously if they are not already present.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedStatutoryRulesAsync(cancellationToken);
        await SeedFileLayoutsAsync(cancellationToken);
    }

    private async Task SeedStatutoryRulesAsync(CancellationToken cancellationToken)
    {
        if (await _context.StatutoryRules.AnyAsync(cancellationToken))
        {
            return;
        }

        var rules = new List<StatutoryRule>
        {
            StatutoryRule.Create("FNPF", "FNPF_EE_RATE", "0.0800", "FNPF Employee Contribution Rate (8%)", new DateTime(2020, 1, 1)),
            StatutoryRule.Create("FNPF", "FNPF_ER_RATE", "0.1000", "FNPF Employer Contribution Rate (10%)", new DateTime(2020, 1, 1)),
            StatutoryRule.Create("FRCS", "PAYE_TAX_FREE_THRESHOLD", "30000.00", "Fiji PAYE Tax Free Threshold ($30,000)", new DateTime(2020, 1, 1)),
            StatutoryRule.Create("FRCS", "PAYE_BRACKET_1_RATE", "0.1800", "Fiji PAYE bracket 1 tax rate (18%)", new DateTime(2020, 1, 1)),
            StatutoryRule.Create("FRCS", "PAYE_BRACKET_1_MIN", "30000.01", "Fiji PAYE bracket 1 minimum threshold", new DateTime(2020, 1, 1)),
            StatutoryRule.Create("FRCS", "PAYE_BRACKET_1_MAX", "50000.00", "Fiji PAYE bracket 1 maximum threshold", new DateTime(2020, 1, 1)),
            StatutoryRule.Create("FRCS", "PAYE_BRACKET_2_RATE", "0.2000", "Fiji PAYE bracket 2 tax rate (20%)", new DateTime(2020, 1, 1)),
            StatutoryRule.Create("FRCS", "PAYE_BRACKET_2_MIN", "50000.01", "Fiji PAYE bracket 2 minimum threshold", new DateTime(2020, 1, 1)),
        };

        foreach (var rule in rules)
        {
            await _context.StatutoryRules.AddAsync(rule, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFileLayoutsAsync(CancellationToken cancellationToken)
    {
        if (await _context.FileLayoutDefinitions.AnyAsync(cancellationToken))
        {
            return;
        }

        var layouts = new List<FileLayoutDefinition>
        {
            // BSP Layout
            FileLayoutDefinition.Create(
                ownerCode: "BSP",
                layoutType: "DirectCredit",
                headerTemplate: "H,{CompanyName},{CompanyAccount},{PaymentDate:yyyyMMdd},{Reference}",
                detailTemplate: "D,{EmployeeName},{BankAccountNumber},{Amount:F2}",
                footerTemplate: "F,{TotalAmount:F2},{TotalCount}",
                columnDelimiter: ',',
                fileExtension: "csv"
            ),
            // ANZ Layout
            FileLayoutDefinition.Create(
                ownerCode: "ANZ",
                layoutType: "DirectCredit",
                headerTemplate: "ANZ-HEADER|{CompanyName}|{CompanyAccount}|{PaymentDate:yyyy-MM-dd}",
                detailTemplate: "ANZ-DETAIL|{EmployeeName}|{BankAccountNumber}|{Amount:F2}|{EmployeeId}",
                footerTemplate: "ANZ-FOOTER|{TotalAmount:F2}|{TotalCount}",
                columnDelimiter: '|',
                fileExtension: "dat"
            ),
            // Westpac Layout
            FileLayoutDefinition.Create(
                ownerCode: "WBC",
                layoutType: "DirectCredit",
                headerTemplate: "WBC_HEADER,{CompanyName},{CompanyAccount},{PaymentDate:ddMMyy}",
                detailTemplate: "WBC_DETAIL,{EmployeeName},{BankAccountNumber},{Amount:F2},{Reference}",
                footerTemplate: "WBC_FOOTER,{TotalAmount:F2},{TotalCount}",
                columnDelimiter: ',',
                fileExtension: "txt"
            ),
            // BRED Layout
            FileLayoutDefinition.Create(
                ownerCode: "BRED",
                layoutType: "DirectCredit",
                headerTemplate: "BRED-HEADER,{CompanyName},{PaymentDate:yyyyMMdd}",
                detailTemplate: "BRED-PAYMENT,{EmployeeName},{BankAccountNumber},{Amount:F2}",
                footerTemplate: "BRED-TRAILER,{TotalAmount:F2}",
                columnDelimiter: ',',
                fileExtension: "csv"
            ),
            // HFC Layout
            FileLayoutDefinition.Create(
                ownerCode: "HFC",
                layoutType: "DirectCredit",
                headerTemplate: "HFC_HEADER|{CompanyName}|{PaymentDate:ddMMyyyy}",
                detailTemplate: "HFC_TRANS|{EmployeeName}|{BankAccountNumber}|{Amount:F2}|{Reference}",
                footerTemplate: "HFC_FOOTER|{TotalAmount:F2}|{TotalCount}",
                columnDelimiter: '|',
                fileExtension: "txt"
            ),
            // Kontiki Layout
            FileLayoutDefinition.Create(
                ownerCode: "KNTK",
                layoutType: "DirectCredit",
                headerTemplate: "KONTIKI_START,{CompanyName},{PaymentDate:yyyyMMdd}",
                detailTemplate: "KONTIKI_PAY,{EmployeeName},{BankAccountNumber},{Amount:F2}",
                footerTemplate: "KONTIKI_END,{TotalAmount:F2},{TotalCount}",
                columnDelimiter: ',',
                fileExtension: "csv"
            )
        };

        foreach (var layout in layouts)
        {
            await _context.FileLayoutDefinitions.AddAsync(layout, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
