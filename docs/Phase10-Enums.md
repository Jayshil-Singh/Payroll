# Phase 10 — Enums & Shared Value Objects Specification

This document details the shared enumerations introduced in **Phase 10: Company Setup Wizard** to govern wizard steps, bank account types, calendar models, frequency codes, approver roles, and audit statuses.

---

## 1. Module Enumerations

### 1.1 WizardStep
Defines the sequential navigation boundaries of the Guided Setup Wizard.
*   **Location**: `FijiPayroll.Domain.Enumerations.WizardStep`
*   **Values**:
    *   `Welcome` (1): The entry page welcoming administrators.
    *   `CompanyDetails` (2): Profile setup fields (TIN, Trading Name, etc.).
    *   `FiscalCalendar` (3): Generation of financial year limits.
    *   `PayrollFrequency` (4): Frequency assignments and cutoffs.
    *   `BankConfiguration` (5): Mappings of corporate banking accounts.
    *   `Approvers` (6): Allocation of HR, Finance, and Payroll approval roles.
    *   `Validation` (7): Dry-run validation checks checklist.
    *   `Completed` (8): Setup finished status.

### 1.2 BankAccountType
Specifies the operational categorization of company bank accounts.
*   **Location**: `FijiPayroll.Domain.Enumerations.BankAccountType`
*   **Values**:
    *   `Operating` (1): General utility disbursements.
    *   `Payroll` (2): Employee salaries distributions.
    *   `Tax` (3): Withholding tax/PAYE deposits.
    *   `FNPF` (4): FNPF contributions remittance.
    *   `Savings` (5): Internal reserve accruals.
    *   `Trust` (6): Statutory escrow holdings.
    *   `Custom` (7): Tailored classifications.

### 1.3 CalendarType
Specifies the period structure of a company's fiscal calendars.
*   **Location**: `FijiPayroll.Domain.Enumerations.CalendarType`
*   **Values**:
    *   `Weekly` (1): 52 or 53 cycles per fiscal year.
    *   `Fortnightly` (2): 26 or 27 cycles per fiscal year.
    *   `Monthly` (3): 12 calendar periods per year.
    *   `Custom` (4): Ad-hoc calendar definitions.

### 1.4 FrequencyCode
Identifies the calculation frequency code mapped to payroll frequency definitions.
*   **Location**: `FijiPayroll.Domain.Enumerations.FrequencyCode`
*   **Values**:
    *   `Weekly` (1): Pay runs executed weekly.
    *   `Fortnightly` (2): Pay runs executed fortnightly.
    *   `BiMonthly` (3): Semi-monthly pay runs (24 periods).
    *   `Monthly` (4): Monthly pay runs.
    *   `Custom` (5): Specialized schedule runs.

### 1.5 ApprovalRole
Defines the operational approvals capability class of a system user.
*   **Location**: `FijiPayroll.Domain.Enumerations.ApprovalRole`
*   **Values**:
    *   `PayrollOfficer` (1): Preparer of preliminary calculation batches.
    *   `PayrollSupervisor` (2): Auditor verifying runs computations.
    *   `FinanceManager` (3): Final signatory authorizing bank release files.
    *   `HRManager` (4): Signatory approving leave, transfers, and contracts.
    *   `Administrator` (5): Root manager holding global configurations authorization.

### 1.6 ExecutionStatus
Tracks the lifecycle execution state of setup execution records and jobs.
*   **Location**: `FijiPayroll.Domain.Enumerations.ExecutionStatus`
*   **Values**:
    *   `Pending` (1)
    *   `Running` (2)
    *   `Completed` (3)
    *   `Failed` (4)
    *   `RolledBack` (5)
    *   `Retrying` (6)
    *   `Cancelled` (7)

### 1.7 SetupAuditStatus
Defines the result state of setup audit log events.
*   **Location**: `FijiPayroll.Domain.Enumerations.SetupAuditStatus`
*   **Values**:
    *   `Success` (1)
    *   `Warning` (2)
    *   `Failed` (3)

### 1.8 SeedCategory
Categorizes seeded parameters and updates packages in migrations.
*   **Location**: `FijiPayroll.Domain.Enumerations.SeedCategory`
*   **Values**:
    *   `Banks` (1)
    *   `Branches` (2)
    *   `LeaveTypes` (3)
    *   `PayrollComponents` (4)
    *   `Reports` (5)
    *   `Roles` (6)
    *   `Permissions` (7)
    *   `Settings` (8)

---

## 2. Serialization & Database Persistence Design

*   **JSON Serialization**: All enums are serialized to/from integers during API operations, matching numeric mapping targets.
*   **EF Core Mapping**: Map to database columns as integer types (`INT`), maintaining clean index ranges and backward compatibility.
