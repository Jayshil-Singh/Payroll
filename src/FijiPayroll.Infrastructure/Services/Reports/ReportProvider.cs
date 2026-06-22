using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.SDK.Interfaces;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Services;

namespace FijiPayroll.Infrastructure.Services.Reports;

/// <summary>
/// Service providing high-quality PDF and Excel reports using ClosedXML and custom raw PDF stream construction.
/// </summary>
public sealed class ReportProvider : IReportProvider
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PayrollDifferenceAnalyzer _differenceAnalyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportProvider"/> class.
    /// </summary>
    public ReportProvider(IUnitOfWork unitOfWork, PayrollDifferenceAnalyzer differenceAnalyzer)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _differenceAnalyzer = differenceAnalyzer ?? throw new ArgumentNullException(nameof(differenceAnalyzer));
    }

    /// <inheritdoc />
    public async Task<byte[]> RenderReportAsync(
        string reportName,
        string format,
        IDictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        int companyId = GetIntParameter(parameters, "@P_CompanyId", "CompanyId");
        int runId = GetIntParameter(parameters, "@P_PayrollRunId", "PayrollRunId");

        var company = await _unitOfWork.Setup.GetCompanyByIdAsync(companyId, cancellationToken);
        if (company == null) throw new InvalidOperationException($"Company with ID {companyId} not found.");

        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(runId, cancellationToken);
        if (run == null) throw new InvalidOperationException($"Payroll run with ID {runId} not found.");

        string normReport = Path.GetFileNameWithoutExtension(reportName);

        if (string.Equals(normReport, "PayrollSummary", StringComparison.OrdinalIgnoreCase))
        {
            return format.Equals("Excel", StringComparison.OrdinalIgnoreCase)
                ? GeneratePayrollSummaryExcel(company, run)
                : GeneratePayrollSummaryPdf(company, run);
        }
        else if (string.Equals(normReport, "PayrollRegister", StringComparison.OrdinalIgnoreCase))
        {
            return format.Equals("Excel", StringComparison.OrdinalIgnoreCase)
                ? GeneratePayrollRegisterExcel(company, run)
                : GeneratePayrollRegisterPdf(company, run);
        }
        else if (string.Equals(normReport, "PayslipBatch", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(normReport, "Payslips", StringComparison.OrdinalIgnoreCase))
        {
            return format.Equals("Excel", StringComparison.OrdinalIgnoreCase)
                ? GeneratePayslipBatchExcel(company, run)
                : GeneratePayslipBatchPdf(company, run);
        }
        else if (string.Equals(normReport, "DepartmentSummary", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(normReport, "PayrollVariance", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(normReport, "Variance", StringComparison.OrdinalIgnoreCase))
        {
            int compareRunId = GetIntParameter(parameters, "@P_PayrollRunBId", "CompareRunId");
            PayrollRun? compareRun = null;
            if (compareRunId > 0)
            {
                compareRun = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(compareRunId, cancellationToken);
            }

            if (compareRun == null)
            {
                var allRunsResult = await _unitOfWork.PayrollRuns.GetPagedAsync(companyId, run.Frequency, null, 1, 1000, cancellationToken);
                compareRun = allRunsResult.Items
                    .Where(r => r.Id != run.Id && r.EndDate < run.StartDate)
                    .OrderByDescending(r => r.EndDate)
                    .FirstOrDefault();
            }

            if (compareRun == null)
            {
                throw new InvalidOperationException("No preceding or comparative run found to calculate variance.");
            }

            var diffReport = _differenceAnalyzer.CompareRuns(compareRun, run);

            return format.Equals("Excel", StringComparison.OrdinalIgnoreCase)
                ? GenerateVarianceExcel(company, run, compareRun, diffReport)
                : GenerateVariancePdf(company, run, compareRun, diffReport);
        }

        throw new NotSupportedException($"Report '{reportName}' is not supported.");
    }

    private int GetIntParameter(IDictionary<string, string> parameters, string key, string fallbackKey)
    {
        if (parameters.TryGetValue(key, out var val) && int.TryParse(val, out var res)) return res;
        if (parameters.TryGetValue(fallbackKey, out val) && int.TryParse(val, out res)) return res;
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EXCEL EXPORTERS
    // ─────────────────────────────────────────────────────────────────────────

    private byte[] GeneratePayrollSummaryExcel(Domain.Entities.Company.Company company, PayrollRun run)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Payroll Summary");

        ws.Cell(1, 1).Value = "Fiji Enterprise Payroll - Payroll Summary Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        ws.Cell(3, 1).Value = "Company:";
        ws.Cell(3, 2).Value = company.LegalName;
        ws.Cell(4, 1).Value = "TIN / FNPF:";
        ws.Cell(4, 2).Value = $"{company.TIN} / {company.FnpfEmployerNumber}";
        ws.Cell(5, 1).Value = "Payroll Period:";
        ws.Cell(5, 2).Value = $"{run.PeriodName} ({run.StartDate:dd/MM/yyyy} - {run.EndDate:dd/MM/yyyy})";
        ws.Cell(6, 1).Value = "Disbursement Date:";
        ws.Cell(6, 2).Value = run.PaymentDate.ToString("dd/MM/yyyy");

        var employees = run.Employees.Where(e => !e.IsSuperseded).ToList();

        ws.Cell(8, 1).Value = "Metric";
        ws.Cell(8, 2).Value = "Value";
        ws.Range(8, 1, 8, 2).Style.Font.Bold = true;
        ws.Range(8, 1, 8, 2).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        ws.Cell(9, 1).Value = "Total Active Employees";
        ws.Cell(9, 2).Value = employees.Count;

        ws.Cell(10, 1).Value = "Total Gross Pay";
        ws.Cell(10, 2).Value = employees.Sum(e => e.GrossPay);
        ws.Cell(10, 2).Style.NumberFormat.Format = "$#,##0.00";

        ws.Cell(11, 1).Value = "Total PAYE Tax";
        ws.Cell(11, 2).Value = employees.Sum(e => e.PayeTax);
        ws.Cell(11, 2).Style.NumberFormat.Format = "$#,##0.00";

        ws.Cell(12, 1).Value = "Total FNPF Employee (8%)";
        ws.Cell(12, 2).Value = employees.Sum(e => e.FnpfEmployeeContribution);
        ws.Cell(12, 2).Style.NumberFormat.Format = "$#,##0.00";

        ws.Cell(13, 1).Value = "Total FNPF Employer (10%)";
        ws.Cell(13, 2).Value = employees.Sum(e => e.FnpfEmployerContribution);
        ws.Cell(13, 2).Style.NumberFormat.Format = "$#,##0.00";

        ws.Cell(14, 1).Value = "Total Net Pay";
        ws.Cell(14, 2).Value = employees.Sum(e => e.NetPay);
        ws.Cell(14, 2).Style.NumberFormat.Format = "$#,##0.00";

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private byte[] GeneratePayrollRegisterExcel(Domain.Entities.Company.Company company, PayrollRun run)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Payroll Register");

        ws.Cell(1, 1).Value = "Payroll Register - " + company.TradingName;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = $"Period: {run.PeriodName} ({run.StartDate:dd/MM/yyyy} - {run.EndDate:dd/MM/yyyy})";

        string[] headers = { "Code/ID", "Employee Name", "TIN", "FNPF No.", "Base Salary", "Gross Pay", "PAYE Tax", "FNPF EE (8%)", "FNPF ER (10%)", "Net Pay" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
            ws.Cell(4, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        }

        var employees = run.Employees.Where(e => !e.IsSuperseded).ToList();
        int row = 5;
        foreach (var emp in employees)
        {
            ws.Cell(row, 1).Value = emp.EmployeeId;
            ws.Cell(row, 2).Value = emp.EmployeeName;
            ws.Cell(row, 3).Value = emp.Tin;
            ws.Cell(row, 4).Value = emp.FnpfNumber;

            ws.Cell(row, 5).Value = emp.BaseSalary;
            ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell(row, 6).Value = emp.GrossPay;
            ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell(row, 7).Value = emp.PayeTax;
            ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell(row, 8).Value = emp.FnpfEmployeeContribution;
            ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell(row, 9).Value = emp.FnpfEmployerContribution;
            ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell(row, 10).Value = emp.NetPay;
            ws.Cell(row, 10).Style.NumberFormat.Format = "$#,##0.00";

            row++;
        }

        // Totals row
        ws.Cell(row, 2).Value = "TOTAL";
        ws.Cell(row, 2).Style.Font.Bold = true;

        for (int col = 5; col <= 10; col++)
        {
            string colLetter = ws.Cell(row, col).Address.ColumnLetter;
            ws.Cell(row, col).FormulaA1 = $"=SUM({colLetter}5:{colLetter}{row - 1})";
            ws.Cell(row, col).Style.Font.Bold = true;
            ws.Cell(row, col).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, col).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, col).Style.Border.BottomBorder = XLBorderStyleValues.Double;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private byte[] GeneratePayslipBatchExcel(Domain.Entities.Company.Company company, PayrollRun run)
    {
        using var workbook = new XLWorkbook();
        var employees = run.Employees.Where(e => !e.IsSuperseded).ToList();

        foreach (var emp in employees)
        {
            string tabName = emp.EmployeeName.Length > 25 ? emp.EmployeeName[..25] : emp.EmployeeName;
            tabName = string.Join("_", tabName.Split(Path.GetInvalidFileNameChars()));
            var ws = workbook.Worksheets.Add(tabName);

            ws.Cell(1, 1).Value = company.TradingName;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;

            ws.Cell(2, 1).Value = $"Employer TIN: {company.TIN} | FNPF ID: {company.FnpfEmployerNumber}";

            ws.Cell(4, 1).Value = "Employee Name:";
            ws.Cell(4, 2).Value = emp.EmployeeName;
            ws.Cell(5, 1).Value = "Employee ID / TIN:";
            ws.Cell(5, 2).Value = $"{emp.EmployeeId} / {emp.Tin}";
            ws.Cell(6, 1).Value = "Department:";
            ws.Cell(6, 2).Value = emp.Department;

            ws.Cell(4, 4).Value = "Pay Period:";
            ws.Cell(4, 5).Value = run.PeriodName;
            ws.Cell(5, 4).Value = "Payment Date:";
            ws.Cell(5, 5).Value = run.PaymentDate.ToString("dd/MM/yyyy");

            ws.Cell(8, 1).Value = "Earnings Component";
            ws.Cell(8, 2).Value = "Amount";
            ws.Cell(8, 4).Value = "Deductions Component";
            ws.Cell(8, 5).Value = "Amount";
            ws.Range(8, 1, 8, 2).Style.Font.Bold = true;
            ws.Range(8, 4, 8, 5).Style.Font.Bold = true;

            int earnRow = 9;
            int dedRow = 9;

            // Base pay
            ws.Cell(earnRow, 1).Value = "Base Salary Rate";
            ws.Cell(earnRow, 2).Value = emp.BaseSalary;
            ws.Cell(earnRow, 2).Style.NumberFormat.Format = "$#,##0.00";
            earnRow++;

            // Allowances & Deductions from Line Items
            foreach (var item in emp.LineItems)
            {
                if (item.ComponentType == ComponentType.Allowance || item.ComponentType == ComponentType.Overtime)
                {
                    ws.Cell(earnRow, 1).Value = item.ComponentName;
                    ws.Cell(earnRow, 2).Value = item.Amount;
                    ws.Cell(earnRow, 2).Style.NumberFormat.Format = "$#,##0.00";
                    earnRow++;
                }
                else if (item.ComponentType == ComponentType.Deduction || item.ComponentType == ComponentType.LoanRepayment || item.ComponentType == ComponentType.LeaveDeduction)
                {
                    ws.Cell(dedRow, 4).Value = item.ComponentName;
                    ws.Cell(dedRow, 5).Value = item.Amount;
                    ws.Cell(dedRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    dedRow++;
                }
            }

            // PAYE and FNPF employee portion are standard deductions
            ws.Cell(dedRow, 4).Value = "PAYE Income Tax";
            ws.Cell(dedRow, 5).Value = emp.PayeTax;
            ws.Cell(dedRow, 5).Style.NumberFormat.Format = "$#,##0.00";
            dedRow++;

            ws.Cell(dedRow, 4).Value = "FNPF Employee Cont. (8%)";
            ws.Cell(dedRow, 5).Value = emp.FnpfEmployeeContribution;
            ws.Cell(dedRow, 5).Style.NumberFormat.Format = "$#,##0.00";
            dedRow++;

            int maxRow = Math.Max(earnRow, dedRow) + 1;
            ws.Cell(maxRow, 1).Value = "Total Gross Pay:";
            ws.Cell(maxRow, 2).FormulaA1 = $"=SUM(B9:B{earnRow - 1})";
            ws.Cell(maxRow, 2).Style.Font.Bold = true;
            ws.Cell(maxRow, 2).Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell(maxRow, 4).Value = "Total Deductions:";
            ws.Cell(maxRow, 5).FormulaA1 = $"=SUM(E9:E{dedRow - 1})";
            ws.Cell(maxRow, 5).Style.Font.Bold = true;
            ws.Cell(maxRow, 5).Style.NumberFormat.Format = "$#,##0.00";

            ws.Cell(maxRow + 2, 4).Value = "NET DISBURSEMENT:";
            ws.Cell(maxRow + 2, 4).Style.Font.Bold = true;
            ws.Cell(maxRow + 2, 5).FormulaA1 = $"=B{maxRow}-E{maxRow}";
            ws.Cell(maxRow + 2, 5).Style.Font.Bold = true;
            ws.Cell(maxRow + 2, 5).Style.Font.FontSize = 12;
            ws.Cell(maxRow + 2, 5).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(maxRow + 2, 5).Style.Border.BottomBorder = XLBorderStyleValues.Double;

            ws.Cell(maxRow + 4, 1).Value = "Employer FNPF Contribution (10%):";
            ws.Cell(maxRow + 4, 2).Value = emp.FnpfEmployerContribution;
            ws.Cell(maxRow + 4, 2).Style.NumberFormat.Format = "$#,##0.00";

            ws.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private byte[] GenerateVarianceExcel(Domain.Entities.Company.Company company, PayrollRun run, PayrollRun compareRun, PayrollDifferenceReport diff)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Variance Report");

        ws.Cell(1, 1).Value = "Payroll Variance Analysis - " + company.TradingName;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = $"Comparing Current: {run.PeriodName} vs Prior: {compareRun.PeriodName}";

        ws.Cell(4, 1).Value = "Summary Metric";
        ws.Cell(4, 2).Value = "Prior Run";
        ws.Cell(4, 3).Value = "Current Run";
        ws.Cell(4, 4).Value = "Variance";
        ws.Range(4, 1, 4, 4).Style.Font.Bold = true;
        ws.Range(4, 1, 4, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        ws.Cell(5, 1).Value = "Gross Pay Summary";
        ws.Cell(5, 2).Value = compareRun.Employees.Where(e => !e.IsSuperseded).Sum(e => e.GrossPay);
        ws.Cell(5, 3).Value = run.Employees.Where(e => !e.IsSuperseded).Sum(e => e.GrossPay);
        ws.Cell(5, 4).Value = diff.TotalGrossDifference;

        ws.Cell(6, 1).Value = "Net Pay Summary";
        ws.Cell(6, 2).Value = compareRun.Employees.Where(e => !e.IsSuperseded).Sum(e => e.NetPay);
        ws.Cell(6, 3).Value = run.Employees.Where(e => !e.IsSuperseded).Sum(e => e.NetPay);
        ws.Cell(6, 4).Value = diff.TotalNetDifference;

        ws.Cell(7, 1).Value = "PAYE Tax Summary";
        ws.Cell(7, 2).Value = compareRun.Employees.Where(e => !e.IsSuperseded).Sum(e => e.PayeTax);
        ws.Cell(7, 3).Value = run.Employees.Where(e => !e.IsSuperseded).Sum(e => e.PayeTax);
        ws.Cell(7, 4).Value = diff.TotalTaxDifference;

        ws.Cell(8, 1).Value = "FNPF Contribution Summary";
        ws.Cell(8, 2).Value = compareRun.Employees.Where(e => !e.IsSuperseded).Sum(e => e.FnpfEmployeeContribution + e.FnpfEmployerContribution);
        ws.Cell(8, 3).Value = run.Employees.Where(e => !e.IsSuperseded).Sum(e => e.FnpfEmployeeContribution + e.FnpfEmployerContribution);
        ws.Cell(8, 4).Value = diff.TotalFnpfDifference;

        for (int r = 5; r <= 8; r++)
        {
            ws.Cell(r, 2).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(r, 4).Style.NumberFormat.Format = "$#,##0.00";
        }

        ws.Cell(10, 1).Value = "Employee Specific Deltas";
        ws.Cell(10, 1).Style.Font.Bold = true;

        string[] headers = { "Emp ID", "Employee Name", "Salary Var", "Gross Var", "PAYE Var", "FNPF Var", "Net Pay Var" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(11, i + 1).Value = headers[i];
            ws.Cell(11, i + 1).Style.Font.Bold = true;
            ws.Cell(11, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        }

        int row = 12;
        foreach (var delta in diff.EmployeeDifferences)
        {
            ws.Cell(row, 1).Value = delta.EmployeeId;
            ws.Cell(row, 2).Value = delta.EmployeeName;
            ws.Cell(row, 3).Value = delta.BaseSalaryDiff;
            ws.Cell(row, 4).Value = delta.GrossPayDiff;
            ws.Cell(row, 5).Value = delta.TaxDiff;
            ws.Cell(row, 6).Value = delta.FnpfDiff;
            ws.Cell(row, 7).Value = delta.NetPayDiff;

            for (int c = 3; c <= 7; c++)
            {
                ws.Cell(row, c).Style.NumberFormat.Format = "$#,##0.00";
            }
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PDF EXPORTERS (Raw PDF Stream construction)
    // ─────────────────────────────────────────────────────────────────────────

    private byte[] GeneratePayrollSummaryPdf(Domain.Entities.Company.Company company, PayrollRun run)
    {
        var employees = run.Employees.Where(e => !e.IsSuperseded).ToList();
        var writer = new SimplePdfWriter();

        writer.AddObject("<< /Type /Catalog /Pages 2 0 R >>");
        writer.AddObject("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        writer.AddObject("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.275 841.889] /Resources << /Font << /F1 4 0 R /F2 6 0 R >> >> /Contents 5 0 R >>");
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var sb = new StringBuilder();
        sb.Append("BT\n");
        sb.Append("/F2 16 Tf 70 780 Td\n");
        sb.Append($"({EscapePdfText($"FIJI ENTERPRISE PAYROLL - PAYROLL SUMMARY REPORT")}) Tj\n");
        
        sb.Append("/F1 10 Tf 0 -25 Td\n");
        sb.Append($"({EscapePdfText($"Company: {company.LegalName}  |  TIN: {company.TIN}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"Pay Period: {run.PeriodName}  |  Payment Date: {run.PaymentDate:dd/MM/yyyy}")}) Tj\n");

        sb.Append("/F2 12 Tf 0 -35 Td\n");
        sb.Append("(1. PAYROLL RUN TOTALS) Tj\n");

        sb.Append("/F1 10 Tf 0 -20 Td\n");
        sb.Append($"({EscapePdfText($"Total Calculated Employees: {employees.Count}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"Total Gross Earnings: {employees.Sum(e => e.GrossPay):C}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"Total Net Pay Disbursed: {employees.Sum(e => e.NetPay):C}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"Total PAYE Tax Withheld: {employees.Sum(e => e.PayeTax):C}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"Total Employee FNPF Contribution (8%): {employees.Sum(e => e.FnpfEmployeeContribution):C}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"Total Employer FNPF Contribution (10%): {employees.Sum(e => e.FnpfEmployerContribution):C}")}) Tj\n");

        sb.Append("/F2 12 Tf 0 -35 Td\n");
        sb.Append("(2. GENERAL DECLARATION) Tj\n");
        sb.Append("/F1 10 Tf 0 -20 Td\n");
        sb.Append($"({EscapePdfText($"Generated by Fiji Enterprise Payroll system at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append("({Sign off / Approved By: __________________________}) Tj\n");

        sb.Append("ET\n");
        sb.Append("1 w 0 G\n");
        sb.Append("60 790 m 530 790 l S\n");
        sb.Append("60 740 m 530 740 l S\n");
        sb.Append("60 50 m 530 50 l S\n");
        sb.Append("BT /F1 8 Tf 270 35 Td (Page 1 of 1) Tj ET\n");

        byte[] contentBytes = Encoding.ASCII.GetBytes(sb.ToString());
        string streamHeader = $"<< /Length {contentBytes.Length} >>\nstream\n";
        string streamFooter = "\nendstream\n";

        var result = new byte[streamHeader.Length + contentBytes.Length + streamFooter.Length];
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamHeader), 0, result, 0, streamHeader.Length);
        Buffer.BlockCopy(contentBytes, 0, result, streamHeader.Length, contentBytes.Length);
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamFooter), 0, result, streamHeader.Length + contentBytes.Length, streamFooter.Length);

        writer.AddObject(result);
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        return writer.Build();
    }

    private byte[] GeneratePayrollRegisterPdf(Domain.Entities.Company.Company company, PayrollRun run)
    {
        var employees = run.Employees.Where(e => !e.IsSuperseded).ToList();
        var writer = new SimplePdfWriter();

        writer.AddObject("<< /Type /Catalog /Pages 2 0 R >>");
        writer.AddObject("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        writer.AddObject("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.275 841.889] /Resources << /Font << /F1 4 0 R /F2 6 0 R >> >> /Contents 5 0 R >>");
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var sb = new StringBuilder();
        sb.Append("BT\n");
        sb.Append("/F2 14 Tf 70 780 Td\n");
        sb.Append($"({EscapePdfText($"FIJI PAYROLL REGISTER - {company.TradingName}")}) Tj\n");
        
        sb.Append("/F1 9 Tf 0 -20 Td\n");
        sb.Append($"({EscapePdfText($"Run Code: {run.RunCode}  |  Period: {run.PeriodName}  |  Payment: {run.PaymentDate:dd/MM/yyyy}")}) Tj\n");

        // Table Header
        sb.Append("/F2 8 Tf 0 -30 Td\n");
        sb.Append($"({EscapePdfText("Name                     TIN       FNPF No.       Gross      PAYE     FNPF(EE)     Net Pay")}) Tj\n");
        
        sb.Append("/F1 8 Tf\n");
        foreach (var emp in employees)
        {
            string nameStr = emp.EmployeeName.PadRight(22)[..22];
            string tinStr = emp.Tin.PadRight(9)[..9];
            string fnpfStr = emp.FnpfNumber.PadRight(10)[..10];
            string grossStr = emp.GrossPay.ToString("F2").PadLeft(10);
            string payeStr = emp.PayeTax.ToString("F2").PadLeft(9);
            string fnpfEeStr = emp.FnpfEmployeeContribution.ToString("F2").PadLeft(10);
            string netStr = emp.NetPay.ToString("F2").PadLeft(12);

            sb.Append("0 -15 Td\n");
            sb.Append($"({EscapePdfText($"{nameStr} {tinStr} {fnpfStr} {grossStr} {payeStr} {fnpfEeStr} {netStr}")}) Tj\n");
        }

        // Totals
        string totGross = employees.Sum(e => e.GrossPay).ToString("F2").PadLeft(10);
        string totPaye = employees.Sum(e => e.PayeTax).ToString("F2").PadLeft(9);
        string totEe = employees.Sum(e => e.FnpfEmployeeContribution).ToString("F2").PadLeft(10);
        string totNet = employees.Sum(e => e.NetPay).ToString("F2").PadLeft(12);
        
        sb.Append("/F2 8 Tf 0 -25 Td\n");
        sb.Append($"({EscapePdfText($"TOTALS{"".PadRight(43)}{totGross} {totPaye} {totEe} {totNet}")}) Tj\n");

        sb.Append("ET\n");
        sb.Append("1 w 0 G\n");
        sb.Append("60 790 m 530 790 l S\n");
        sb.Append("60 720 m 530 720 l S\n");
        sb.Append("60 50 m 530 50 l S\n");
        sb.Append("BT /F1 8 Tf 270 35 Td (Page 1 of 1) Tj ET\n");

        byte[] contentBytes = Encoding.ASCII.GetBytes(sb.ToString());
        string streamHeader = $"<< /Length {contentBytes.Length} >>\nstream\n";
        string streamFooter = "\nendstream\n";

        var result = new byte[streamHeader.Length + contentBytes.Length + streamFooter.Length];
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamHeader), 0, result, 0, streamHeader.Length);
        Buffer.BlockCopy(contentBytes, 0, result, streamHeader.Length, contentBytes.Length);
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamFooter), 0, result, streamHeader.Length + contentBytes.Length, streamFooter.Length);

        writer.AddObject(result);
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        return writer.Build();
    }

    private byte[] GeneratePayslipBatchPdf(Domain.Entities.Company.Company company, PayrollRun run)
    {
        var employees = run.Employees.Where(e => !e.IsSuperseded).ToList();
        var writer = new SimplePdfWriter();

        // 1. Catalog
        writer.AddObject("<< /Type /Catalog /Pages 2 0 R >>");

        // 2. Pages structure
        var pageKids = new StringBuilder();
        for (int i = 0; i < employees.Count; i++)
        {
            pageKids.Append($"{(3 + i * 2)} 0 R ");
        }
        writer.AddObject($"<< /Type /Pages /Kids [{pageKids.ToString().Trim()}] /Count {employees.Count} >>");

        // Font 1 (ID 4 - fallback font ID reference inside catalog lookup)
        // Wait, standardizing object sequence makes page references easier:
        // We will create the pages and content streams dynamically.
        // Let's create:
        // Page 1 (obj ID 3) -> Content 1 (obj ID 4)
        // Page 2 (obj ID 5) -> Content 2 (obj ID 6)
        // etc.
        // And we will append Fonts at the end!
        // Font 1 (Helvetica) will be registered at (3 + 2 * EmpCount)
        // Font 2 (Helvetica-Bold) will be registered at (4 + 2 * EmpCount)
        int font1Id = 3 + 2 * employees.Count;
        int font2Id = 4 + 2 * employees.Count;

        for (int i = 0; i < employees.Count; i++)
        {
            int pageId = 3 + i * 2;
            int contentId = 4 + i * 2;

            // Page Object
            writer.AddObject($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.275 841.889] /Resources << /Font << /F1 {font1Id} 0 R /F2 {font2Id} 0 R >> >> /Contents {contentId} 0 R >>");

            // Content Stream Object
            var emp = employees[i];
            var sb = new StringBuilder();
            sb.Append("BT\n");
            
            // Header
            sb.Append("/F2 14 Tf 70 780 Td\n");
            sb.Append($"({EscapePdfText(company.TradingName)}) Tj\n");
            sb.Append("/F1 8 Tf 0 -15 Td\n");
            sb.Append($"({EscapePdfText($"Employer TIN: {company.TIN} | FNPF Employer ID: {company.FnpfEmployerNumber}")}) Tj\n");

            sb.Append("0 -30 Td\n");
            sb.Append("/F2 11 Tf (EMPLOYEE PAYSLIP) Tj\n");

            // Meta Details block
            sb.Append("/F1 9 Tf 0 -20 Td\n");
            sb.Append($"({EscapePdfText($"Name: {emp.EmployeeName}")}) Tj\n");
            sb.Append("0 -15 Td\n");
            sb.Append($"({EscapePdfText($"Employee ID: {emp.EmployeeId}  |  TIN: {emp.Tin}  |  FNPF No: {emp.FnpfNumber}")}) Tj\n");
            sb.Append("0 -15 Td\n");
            sb.Append($"({EscapePdfText($"Department: {emp.Department}  |  Pay Period: {run.PeriodName}")}) Tj\n");
            sb.Append("0 -15 Td\n");
            sb.Append($"({EscapePdfText($"Pay Date: {run.PaymentDate:dd/MM/yyyy}")}) Tj\n");

            // Earnings Column (X=0, relative) vs Deductions Column
            sb.Append("/F2 10 Tf 0 -35 Td\n");
            sb.Append($"({EscapePdfText("EARNINGS                                  DEDUCTIONS")}) Tj\n");

            // Base pay
            sb.Append("/F1 9 Tf 0 -20 Td\n");
            sb.Append($"({EscapePdfText($"Base Salary Rate: {emp.BaseSalary:C}")}) Tj\n");

            // Draw components
            var allowances = emp.LineItems.Where(l => l.ComponentType == ComponentType.Allowance || l.ComponentType == ComponentType.Overtime).ToList();
            var deductions = emp.LineItems.Where(l => l.ComponentType == ComponentType.Deduction || l.ComponentType == ComponentType.LoanRepayment || l.ComponentType == ComponentType.LeaveDeduction).ToList();

            int maxItems = Math.Max(allowances.Count, deductions.Count + 2); // +2 for PAYE and FNPF employee
            
            for (int k = 0; k < maxItems; k++)
            {
                string earnLine = string.Empty;
                if (k < allowances.Count)
                {
                    earnLine = $"{allowances[k].ComponentName}: {allowances[k].Amount:C}";
                }

                string dedLine = string.Empty;
                if (k == 0)
                {
                    dedLine = $"PAYE Income Tax: {emp.PayeTax:C}";
                }
                else if (k == 1)
                {
                    dedLine = $"FNPF Employee (8%): {emp.FnpfEmployeeContribution:C}";
                }
                else
                {
                    int dedIndex = k - 2;
                    if (dedIndex < deductions.Count)
                    {
                        dedLine = $"{deductions[dedIndex].ComponentName}: {deductions[dedIndex].Amount:C}";
                    }
                }

                sb.Append("0 -15 Td\n");
                string lineText = $"{earnLine.PadRight(35)[..35]} {dedLine}";
                sb.Append($"({EscapePdfText(lineText)}) Tj\n");
            }

            // Totals
            sb.Append("/F2 9 Tf 0 -30 Td\n");
            string totalsStr = $"Total Gross: {emp.GrossPay:C}             Total Deductions: {emp.TotalDeductions + emp.PayeTax + emp.FnpfEmployeeContribution:C}";
            sb.Append($"({EscapePdfText(totalsStr)}) Tj\n");

            sb.Append("/F2 11 Tf 0 -30 Td\n");
            sb.Append($"({EscapePdfText($"NET DISBURSEMENT AMOUNT: {emp.NetPay:C}")}) Tj\n");

            sb.Append("/F1 8 Tf 0 -35 Td\n");
            sb.Append($"({EscapePdfText($"Employer FNPF Contribution (10%): {emp.FnpfEmployerContribution:C}")}) Tj\n");

            sb.Append("ET\n");
            
            // Layout lines
            sb.Append("1 w 0 G\n");
            sb.Append("60 790 m 530 790 l S\n");
            sb.Append("60 730 m 530 730 l S\n");
            sb.Append("60 50 m 530 50 l S\n");
            sb.Append($"BT /F1 8 Tf 260 35 Td (Page {i + 1} of {employees.Count}) Tj ET\n");

            byte[] contentBytes = Encoding.ASCII.GetBytes(sb.ToString());
            string streamHeader = $"<< /Length {contentBytes.Length} >>\nstream\n";
            string streamFooter = "\nendstream\n";

            var resultBytes = new byte[streamHeader.Length + contentBytes.Length + streamFooter.Length];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamHeader), 0, resultBytes, 0, streamHeader.Length);
            Buffer.BlockCopy(contentBytes, 0, resultBytes, streamHeader.Length, contentBytes.Length);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamFooter), 0, resultBytes, streamHeader.Length + contentBytes.Length, streamFooter.Length);

            writer.AddObject(resultBytes);
        }

        // Add Fonts
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        return writer.Build();
    }

    private byte[] GenerateVariancePdf(Domain.Entities.Company.Company company, PayrollRun run, PayrollRun compareRun, PayrollDifferenceReport diff)
    {
        var writer = new SimplePdfWriter();

        writer.AddObject("<< /Type /Catalog /Pages 2 0 R >>");
        writer.AddObject("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        writer.AddObject("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.275 841.889] /Resources << /Font << /F1 4 0 R /F2 6 0 R >> >> /Contents 5 0 R >>");
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var sb = new StringBuilder();
        sb.Append("BT\n");
        sb.Append("/F2 14 Tf 70 780 Td\n");
        sb.Append($"({EscapePdfText($"PAYROLL VARIANCE REPORT - {company.TradingName}")}) Tj\n");
        
        sb.Append("/F1 9 Tf 0 -20 Td\n");
        sb.Append($"({EscapePdfText($"Comparing Current Run: {run.PeriodName} vs Prior Run: {compareRun.PeriodName}")}) Tj\n");

        sb.Append("/F2 11 Tf 0 -35 Td\n");
        sb.Append("(1. SUMMARY LEVEL DELTAS) Tj\n");

        sb.Append("/F1 9 Tf 0 -20 Td\n");
        sb.Append($"({EscapePdfText($"Gross Pay Delta: {diff.TotalGrossDifference:C}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"Net Pay Delta: {diff.TotalNetDifference:C}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"PAYE Tax Delta: {diff.TotalTaxDifference:C}")}) Tj\n");
        sb.Append("0 -15 Td\n");
        sb.Append($"({EscapePdfText($"FNPF Combined Contribution Delta: {diff.TotalFnpfDifference:C}")}) Tj\n");

        sb.Append("/F2 11 Tf 0 -35 Td\n");
        sb.Append("(2. EMPLOYEE LEVEL DELTAS) Tj\n");

        sb.Append("/F2 8 Tf 0 -20 Td\n");
        sb.Append($"({EscapePdfText("Name                     Salary Diff     Gross Diff      PAYE Diff      Net Diff")}) Tj\n");

        sb.Append("/F1 8 Tf\n");
        foreach (var delta in diff.EmployeeDifferences.Take(25)) // Max 25 rows in PDF for page alignment
        {
            string nameStr = delta.EmployeeName.PadRight(22)[..22];
            string salStr = delta.BaseSalaryDiff.ToString("F2").PadLeft(12);
            string grossStr = delta.GrossPayDiff.ToString("F2").PadLeft(12);
            string taxStr = delta.TaxDiff.ToString("F2").PadLeft(12);
            string netStr = delta.NetPayDiff.ToString("F2").PadLeft(12);

            sb.Append("0 -15 Td\n");
            sb.Append($"({EscapePdfText($"{nameStr} {salStr} {grossStr} {taxStr} {netStr}")}) Tj\n");
        }

        sb.Append("ET\n");
        sb.Append("1 w 0 G\n");
        sb.Append("60 790 m 530 790 l S\n");
        sb.Append("60 730 m 530 730 l S\n");
        sb.Append("60 50 m 530 50 l S\n");
        sb.Append("BT /F1 8 Tf 270 35 Td (Page 1 of 1) Tj ET\n");

        byte[] contentBytes = Encoding.ASCII.GetBytes(sb.ToString());
        string streamHeader = $"<< /Length {contentBytes.Length} >>\nstream\n";
        string streamFooter = "\nendstream\n";

        var result = new byte[streamHeader.Length + contentBytes.Length + streamFooter.Length];
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamHeader), 0, result, 0, streamHeader.Length);
        Buffer.BlockCopy(contentBytes, 0, result, streamHeader.Length, contentBytes.Length);
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(streamFooter), 0, result, streamHeader.Length + contentBytes.Length, streamFooter.Length);

        writer.AddObject(result);
        writer.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        return writer.Build();
    }

    private static string EscapePdfText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c == '(' || c == ')' || c == '\\') sb.Append('\\').Append(c);
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private sealed class SimplePdfWriter
    {
        private readonly List<byte[]> _objects = [];
        private readonly List<long> _offsets = [];

        public void AddObject(string content)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(content + "\n");
            AddObject(bytes);
        }

        public void AddObject(byte[] bytes)
        {
            _objects.Add(bytes);
        }

        public byte[] Build()
        {
            using var ms = new MemoryStream();
            byte[] header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
            ms.Write(header, 0, header.Length);

            for (int i = 0; i < _objects.Count; i++)
            {
                _offsets.Add(ms.Position);

                byte[] objHeader = Encoding.ASCII.GetBytes($"{(i + 1)} 0 obj\n");
                ms.Write(objHeader, 0, objHeader.Length);

                byte[] objBody = _objects[i];
                ms.Write(objBody, 0, objBody.Length);

                byte[] objFooter = Encoding.ASCII.GetBytes("endobj\n");
                ms.Write(objFooter, 0, objFooter.Length);
            }

            long xrefOffset = ms.Position;

            StringBuilder xrefBuilder = new StringBuilder();
            xrefBuilder.Append("xref\n");
            xrefBuilder.Append($"0 {(_objects.Count + 1)}\n");
            xrefBuilder.Append("0000000000 65535 f \n");
            for (int i = 0; i < _objects.Count; i++)
            {
                xrefBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0:D10} 00000 n \n", _offsets[i]));
            }

            xrefBuilder.Append("trailer\n");
            xrefBuilder.Append($"<< /Size {(_objects.Count + 1)} /Root 1 0 R >>\n");
            xrefBuilder.Append("startxref\n");
            xrefBuilder.Append($"{xrefOffset}\n");
            xrefBuilder.Append("%%EOF\n");

            byte[] trailerBytes = Encoding.ASCII.GetBytes(xrefBuilder.ToString());
            ms.Write(trailerBytes, 0, trailerBytes.Length);

            return ms.ToArray();
        }
    }
}
