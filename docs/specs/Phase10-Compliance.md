# Phase 10 — FRCS / FNPF / Bank Module Specification

**Version:** 1.0.0  
**Date:** June 2026  
**Owner:** Senior Payroll Specialist + Senior C# Developer  

---

> **See also:** [FRCS.md](../FRCS.md), [FNPF.md](../FNPF.md), [BankFiles.md](../BankFiles.md)
> 
> This phase spec consolidates the three compliance modules into a unified UI/workflow specification.

---

## 1. Compliance Module — UI Overview

The Compliance module is accessible from the main navigation under:  
`Compliance` → `FRCS (MER)`, `FNPF`, `Bank Files`

---

## 2. FRCS Module UX

### 2.1 FRCS Dashboard

```
[Compliance → FRCS Monthly Employer Return]

Company: Pacific Supplies Ltd                    Year: 2026

Submission Status:

  Jan 2026  ✅ Submitted  14 Feb 2026    46 employees  $12,450 PAYE
  Feb 2026  ✅ Submitted  12 Mar 2026    47 employees  $12,800 PAYE
  Mar 2026  ✅ Submitted  14 Apr 2026    47 employees  $12,700 PAYE
  Apr 2026  ✅ Submitted  13 May 2026    48 employees  $13,100 PAYE
  May 2026  ✅ Submitted  14 Jun 2026    48 employees  $13,200 PAYE
  Jun 2026  ⚠️ Due: 15 Jul 2026          DUE IN 30 DAYS
  Jul–Dec   ──  Not yet due

[Generate MER for June 2026]
```

### 2.2 FRCS MER Generation Workflow

1. User selects Month/Year
2. System loads all payroll runs for the period
3. System aggregates per-employee data
4. System validates data
5. System shows preview
6. User generates PDF and/or CSV
7. User marks as submitted

### 2.3 FRCS Validation Screen

```
FRCS Validation — June 2026

✅ 3 payroll runs found for June 2026
✅ All runs are in Paid status
✅ 48 employees to be declared
⚠️ 2 employees have no TIN recorded:
   • Mary Brown (E015) — TIN missing
   • David Lee (E032) — TIN missing
   These employees will be included without TIN.
   FRCS may reject their records.

[View and Fix Employees]   [Continue Anyway]   [Cancel]
```

### 2.4 FRCS Data Grid Preview

Before generating the file, user sees:

| TIN | Surname | First Name | Gross | Allowances | PAYE |
|-----|---------|-----------|-------|-----------|------|
| 123456789 | Smith | John | 4,500.00 | 500.00 | 450.00 |
| — | Brown | Mary | 3,800.00 | 0.00 | 342.00 |
| ... | | | | | |
| **Total** | | **48 employees** | **$215,000.00** | **$12,000.00** | **$19,800.00** |

[Export CSV]   [Generate PDF Report]   [Mark as Submitted]

---

## 3. FNPF Module UX

### 3.1 FNPF Dashboard

```
[Compliance → FNPF Contributions]

Company: Pacific Supplies Ltd

  Jan 2026  ✅ Submitted  Last day Feb   $28,440.00 total contribution
  Feb 2026  ✅ Submitted  Last day Mar   $28,800.00 total contribution
  ...
  Jun 2026  ⚠️ Due: 31 Jul 2026          DUE IN 46 DAYS

[Generate FNPF File for June 2026]
```

### 3.2 FNPF Validation Screen

```
FNPF Validation — June 2026

✅ 48 employees included
✅ All employees have FNPF numbers
✅ Employee contributions: $15,120.00  (8%)
✅ Employer contributions: $18,900.00  (10%)
✅ Total to remit:         $34,020.00

No issues found.

[Preview Data]   [Generate CSV File]   [Generate PDF]   [Cancel]
```

### 3.3 FNPF Contribution Preview Grid

| FNPF # | Employee Name | FNPF-Applicable Gross | Employee 8% | Employer 10% | Total |
|--------|--------------|----------------------|------------|-------------|-------|
| 987654321 | SMITH JOHN | 4,500.00 | 360.00 | 450.00 | 810.00 |
| 123456789 | JONES MARY | 3,800.00 | 304.00 | 380.00 | 684.00 |
| ... | | | | | |
| **Total** | **48 employees** | **$189,000.00** | **$15,120.00** | **$18,900.00** | **$34,020.00** |

---

## 4. Bank Files Module UX

### 4.1 Bank Files Dashboard

```
[Compliance → Bank Files]

Company: Pacific Supplies Ltd

Recently Generated Bank Files:
  30/06/2026  Monthly Run Jun 2026  BSP   48 payments  $172,000.00  ✅ Generated
  31/05/2026  Monthly Run May 2026  BSP   48 payments  $168,000.00  ✅ Generated
  30/04/2026  Monthly Run Apr 2026  BSP   47 payments  $164,500.00  ✅ Generated
```

### 4.2 Bank File Generation Screen

```
Generate Bank File

Payroll Run:     Monthly Run — June 2026  (Approved) ✅
Payment Date:    [30/06/2026] (must be a business day)
Bank:            [Bank South Pacific (BSP) ▾]
Company Account: [1234567890 — Pacific Supplies BSP Operating Account ▾]
Output File:     [Browse...] C:\Payroll\Output\BSP_PAYROLL_PSL_20260630.csv

Payment Summary:
  Employees: 48
  Bank:      BSP:       43 employees   $162,400.00
             ANZ:        5 employees   $ 21,500.00  ← different bank!
  Total Net Pay:        48 employees  $183,900.00

⚠️ 5 employees bank with ANZ. You will need to generate a separate ANZ file.

Employees with missing bank accounts: None ✅

[Cancel]    [Preview Payments]    [Generate BSP File]
```

### 4.3 Payment Preview Grid

Before generating the file:

| Employee | Bank | Account # | Account Name | Net Pay |
|---------|------|-----------|-------------|---------|
| John Smith | BSP | XXXXXXXXXX | John Smith | $3,934.66 |
| Mary Jones | ANZ | XXXXXXXXXX | Mary Jones | $3,240.00 |
| ... | | | | |
| **Total** | | | | **$183,900.00** |

[Export Preview to Excel]   [Generate File]   [Cancel]

---

## 5. Reconciliation Views

### 5.1 FRCS Reconciliation Check

Accessed via: `Compliance → FRCS → Reconciliation`

```
FRCS Reconciliation — June 2026

Payroll Run Data:
  Run 1 (Weekly 20 Jun):   PAYE = $4,200.00   Employees: 48
  Run 2 (Weekly 27 Jun):   PAYE = $4,100.00   Employees: 47
  Total from runs:          PAYE = $8,300.00

MER Declared:
  June 2026 MER:            PAYE = $8,300.00

VARIANCE:                         $0.00   ✅ Reconciled

[Print Reconciliation Report]
```

### 5.2 FNPF Reconciliation Check

Similar structure showing:
- Sum from payroll run details
- Sum submitted to FNPF
- Variance (must be $0.00)

---

## 6. Data Mapping Specifications

### 6.1 FRCS MER Data Mapping

| FRCS Field | System Source |
|-----------|--------------|
| Employer TIN | `Companies.TIN` |
| Employee TIN | `Employees.FijiTIN` |
| Surname | `Employees.LastName` (UPPERCASE) |
| First Name | `Employees.FirstName` (UPPERCASE) |
| Gross Salary | SUM(`PayrollRunDetails.GrossPay`) for period |
| Allowances | SUM(`PayrollRunComponentDetails.Amount` WHERE `IsAllowance=true`) |
| PAYE Deducted | SUM(`PayrollRunDetails.PAYEAmount`) for period |
| Period Month | Month number from payroll run period |
| Period Year | Year from payroll run period |

### 6.2 FNPF Contribution Data Mapping

| FNPF Field | System Source |
|-----------|--------------|
| Employer Number | `Companies.FNPFEmployerNumber` |
| Employer Name | `Companies.CompanyName` |
| Period | MM/YYYY from payroll run |
| Employee FNPF # | `Employees.FNPFNumber` |
| Employee Name | `Employees.LastName + ' ' + Employees.FirstName` |
| Employee Contribution | SUM(`PayrollRunDetails.FNPFEmployeeAmount`) |
| Employer Contribution | SUM(`PayrollRunDetails.FNPFEmployerAmount`) |
| Total Contribution | Employee + Employer |

### 6.3 Bank File Data Mapping

| Bank Field | System Source |
|-----------|--------------|
| Employee Account | `EmployeeBankAccounts.AccountNumber` |
| Employee Name | `EmployeeBankAccounts.AccountName` |
| Amount | `PayrollRunDetails.NetPay` (split if multiple accounts) |
| Reference | `PayrollRuns.RunName` (truncated to bank max) |
| Payment Date | `PayrollRuns.PaymentDate` |

---

## 7. Audit Requirements

| Action | Audit Record |
|--------|-------------|
| FRCS file generated | Entity: FRCSSubmission, Action: Generate, User, Timestamp, Month/Year |
| FRCS marked submitted | Entity: FRCSSubmission, Action: Submit |
| FNPF file generated | Entity: FNPFSubmission, Action: Generate, User, Timestamp, Month/Year |
| Bank file generated | Entity: BankFile, Action: Generate, User, Timestamp, File name, Total amount |
| Bank file re-generated | Entity: BankFile, Action: Regenerate, User, Reason |
| Payroll run marked Paid | Entity: PayrollRun, Action: MarkPaid |

---

## 8. Error Handling

| Error | User Message | System Action |
|-------|-------------|--------------|
| No payroll runs in period | "No payroll runs found for [Month Year]. Please complete payroll before generating compliance files." | Block generation |
| Runs not all in Paid status | "[N] payroll runs are not marked as Paid. Do you want to continue with available runs?" | Allow override with warning |
| File write permission denied | "Cannot write to [path]. Please check that the output folder exists and you have write permission." | Show folder picker |
| FNPF number invalid format | "FNPF number for [employee name] has an invalid format. Please correct and retry." | Block generation |
| Zero contribution employee | "Employee [name] has zero FNPF contribution. They will be excluded from the FNPF file." | Auto-exclude + log warning |

---

*Document maintained by: Senior Payroll Specialist*  
*Last updated: June 2026*
