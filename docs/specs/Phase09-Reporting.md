# Phase 09 — SSRS Reporting Framework Specification

**Version:** 1.0.0  
**Date:** June 2026  
**Owner:** Senior SQL Server Database Architect + Senior C# Developer  

---

## 1. Overview

The reporting layer uses **SQL Server Reporting Services (SSRS)** to deliver enterprise-grade, paginated reports. Reports are rendered within the WPF application via the SSRS ReportViewer control and can be exported to multiple formats.

---

## 2. SSRS Infrastructure

### 2.1 Report Server Configuration

| Setting | Value |
|---------|-------|
| Report Server URL | `http://[servername]/ReportServer` |
| Report Manager URL | `http://[servername]/Reports` |
| Authentication | Windows Auth (inherits from AD) or SQL Auth |
| Execution Account | Dedicated service account with db_datareader |
| Report Timeout | 300 seconds (5 minutes) |
| Rendering Format | Default: PDF; available: Excel, Word, CSV |

### 2.2 Report Deployment Folder Structure

```
SSRS Report Server
└── FijiPayroll/
    ├── DataSources/
    │   └── FijiPayrollDS (shared data source)
    ├── Payroll/
    │   ├── PayrollRegister.rdl
    │   ├── PayrollSummary.rdl
    │   ├── PayslipSingle.rdl
    │   └── PayslipBatch.rdl
    ├── Employee/
    │   ├── EmployeeListing.rdl
    │   ├── EmployeeDetailReport.rdl
    │   └── EmployeeVarianceReport.rdl
    ├── Leave/
    │   ├── LeaveSummary.rdl
    │   ├── LeaveLiability.rdl
    │   └── LeaveCalendar.rdl
    ├── Loans/
    │   └── LoanSummary.rdl
    ├── Compliance/
    │   ├── FRCSMonthlyReturn.rdl
    │   └── FNPFContributionReport.rdl
    ├── Financial/
    │   ├── BankSummary.rdl
    │   └── DepartmentSummary.rdl
    └── Audit/
        └── AuditTrailReport.rdl
```

---

## 3. Report Naming Standards

| Standard | Rule | Example |
|----------|------|---------|
| File name | PascalCase, descriptive | `PayrollRegister.rdl` |
| Report title | Title case, displayed to user | `Payroll Register Report` |
| Stored procedure | `usp_Report_[ReportName]` | `usp_Report_PayrollRegister` |
| Parameter naming | PascalCase prefix `@P_` | `@P_CompanyId`, `@P_PeriodFrom` |

---

## 4. Standard Report Parameters

All reports include these standard parameters:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `@P_CompanyId` | INT | Yes | Scopes report to one company |
| `@P_PeriodFrom` | DATE | Depends | Start of date range |
| `@P_PeriodTo` | DATE | Depends | End of date range |
| `@P_PayrollRunId` | INT | Depends | Specific payroll run |
| `@P_DepartmentId` | INT | No | Filter by department |
| `@P_BranchId` | INT | No | Filter by branch |
| `@P_EmployeeId` | INT | No | Single employee |

---

## 5. Report Specifications

---

### 5.1 Payroll Register

**Purpose:** Complete list of all employee pay in a payroll run.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PayrollRunId` (required)
- `@P_DepartmentId` (optional)

**Columns:**
| Column | Description |
|--------|-------------|
| Employee Code | Employee identifier |
| Employee Name | Full name |
| Department | Department name |
| Branch | Branch name |
| Pay Type | Salary/Hourly/Daily |
| Gross Pay | Total gross earnings |
| PAYE | Tax deducted |
| FNPF Employee | Employee contribution |
| FNPF Employer | Employer contribution |
| Other Deductions | Loans + voluntary |
| Net Pay | Take-home pay |

**Grouping:** By Department → Branch → Employee  
**Subtotals:** Per Department, Per Branch  
**Grand Total:** Last row  
**Sort:** By Department, then Employee Last Name  

**Data Source:** `usp_Report_PayrollRegister`

---

### 5.2 Payroll Summary

**Purpose:** High-level summary of a payroll run.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PayrollRunId` (required)

**Contents:**
- Run metadata (run name, period, payment date, status)
- Total employees processed
- Total gross pay
- Total PAYE
- Total FNPF (employee + employer)
- Total other deductions
- Total net pay
- Breakdown by department (table)
- Breakdown by pay type (table)
- Comparison to previous run (variance)

**Format:** Single page, landscape, executive summary style

---

### 5.3 Payslip (Single Employee)

**Purpose:** Individual payslip for one employee for one run.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PayrollRunId` (required)
- `@P_EmployeeId` (required)

**Contents:**
- Company letterhead
- Employee details
- Earnings breakdown (component by component)
- Deductions breakdown
- Net pay
- YTD totals (gross, PAYE, FNPF)
- Employer FNPF contribution
- Leave balances (as at payment date)

**Format:** A4 portrait, 1 page per payslip

---

### 5.4 Payslip (Batch — All Employees)

**Purpose:** Bulk generate all payslips for a run.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PayrollRunId` (required)

**Output:** Multi-page PDF (one payslip per page, using page break)  
**Performance Note:** Use SSRS pagination to handle 1,000+ employees

---

### 5.5 Employee Listing

**Purpose:** Current employee master list.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_DepartmentId` (optional)
- `@P_BranchId` (optional)
- `@P_EmploymentStatus` (optional, default: Active)

**Columns:**
| Column | Description |
|--------|-------------|
| Employee Code | |
| Full Name | |
| Department | |
| Branch | |
| Position | |
| Employment Type | |
| Start Date | |
| Pay Type | |
| Pay Rate | Annual/Hourly/Daily |
| FNPF # | |
| TIN | |
| Status | |

**Sort:** Department → Last Name  
**Grouping:** By Department with count per department

---

### 5.6 Leave Summary

**Purpose:** Leave taken and balances per employee.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PeriodFrom` (required)
- `@P_PeriodTo` (required)
- `@P_LeaveTypeId` (optional)
- `@P_EmployeeId` (optional)

**Columns:**
| Column | Description |
|--------|-------------|
| Employee | Name and code |
| Leave Type | |
| Opening Balance | Balance at period start |
| Accrued | Leave accrued in period |
| Taken | Days taken in period |
| Closing Balance | Balance at period end |

---

### 5.7 Leave Liability

**Purpose:** Financial liability of outstanding leave balances.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_AsAtDate` (required)
- `@P_DepartmentId` (optional)

**Columns:**
| Column | Description |
|--------|-------------|
| Employee | |
| Annual Leave Balance | Days |
| Annual Leave $ Value | Balance × Daily Rate |
| Sick Leave Balance | Days |
| Total Leave Liability $ | Sum of all leave values |

**Summary:** Total liability by department + grand total

---

### 5.8 Loan Summary

**Purpose:** Outstanding loan balances and repayment history.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_AsAtDate` (required)
- `@P_EmployeeId` (optional)

**Columns:**
| Column | Description |
|--------|-------------|
| Employee | |
| Loan Name | |
| Original Amount | |
| Total Repaid | |
| Outstanding Balance | |
| Monthly Instalment | |
| Projected Payoff Date | |

---

### 5.9 Bank Summary

**Purpose:** Bank payment summary for a payroll run.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PayrollRunId` (required)

**Sections:**
- By bank: Total employees, total amount per bank
- By department: Total net pay
- Individual listing: Employee, bank, account, amount

**Purpose:** Reconcile bank file totals before submission.

---

### 5.10 Audit Trail Report

**Purpose:** Full audit log of system actions.

**Parameters:**
- `@P_CompanyId` (optional)
- `@P_PeriodFrom` (required)
- `@P_PeriodTo` (required)
- `@P_UserId` (optional)
- `@P_EntityName` (optional)
- `@P_Action` (optional)

**Columns:**
| Column | Description |
|--------|-------------|
| Timestamp | Date + time |
| User | Username |
| Action | Create/Update/Delete/Login/etc. |
| Module | Entity name |
| Record | Entity ID |
| Old Value | Summary of what changed |
| New Value | New value |

**Note:** Sensitive columns (passwords) are never shown; they are masked as `[REDACTED]`

---

### 5.11 Variance Report

**Purpose:** Compare two payroll runs side by side.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PayrollRunId1` (required — current run)
- `@P_PayrollRunId2` (required — comparison run)

**Columns:**
| Column | Description |
|--------|-------------|
| Employee | |
| Current Gross | |
| Previous Gross | |
| Gross Variance $ | |
| Gross Variance % | |
| Current Net | |
| Previous Net | |
| Net Variance $ | |
| Current PAYE | |
| Previous PAYE | |
| PAYE Variance $ | |

**Highlight:** Rows with variance > 10% flagged in amber; > 50% flagged in red

---

### 5.12 Department Summary

**Purpose:** Total payroll cost by department for a period.

**Parameters:**
- `@P_CompanyId` (required)
- `@P_PeriodFrom` (required)
- `@P_PeriodTo` (required)

**Chart:** Bar chart of total gross per department  
**Table:** Department | Employee Count | Total Gross | Total PAYE | Total FNPF | Total Net

---

## 6. Export Formats

| Format | Use Case |
|--------|---------|
| PDF | Payslips, compliance reports, archiving |
| Excel (.xlsx) | Data analysis, further manipulation |
| Word (.docx) | Customisable reports |
| CSV | System integration, bulk data |
| Print | Direct printer output |

---

## 7. Performance Optimisation

| Technique | Application |
|-----------|------------|
| Indexed views for reporting | `reporting.*` schema views |
| Pre-aggregated data | Payroll run totals stored in header record |
| Pagination in large reports | SSRS page size limits + SQL TOP/OFFSET |
| Report caching | SSRS snapshot caching for completed runs |
| Async rendering | Run report in background for large datasets |
| Dapper for report queries | Bypass EF Core for complex read-only queries |

---

## 8. Security Model

| Control | Implementation |
|---------|---------------|
| Company isolation | `@P_CompanyId` parameter on all reports |
| Permission check | Checked before report dialog opens (WPF layer) |
| SSRS access | Report Server accessed via service account only |
| Data source credentials | Stored in SSRS as encrypted service account |
| Export security | PDF reports are not password-protected by default (configurable) |

---

## 9. WPF Integration

Reports are displayed in the WPF app using the Microsoft.Reporting.WinForms `ReportViewer` control embedded in a WPF frame.

**Report Toolbar Features:**
- Navigate pages (first/prev/next/last)
- Zoom control
- Search text
- Export (dropdown of formats)
- Print
- Refresh

**Report Parameters UI:**
- Built dynamically from report parameter definitions
- Date pickers for date parameters
- Dropdowns for limited-value parameters
- Company and run pre-filled from current context

---

*Document maintained by: Senior SQL Server Database Architect*  
*Last updated: June 2026*
