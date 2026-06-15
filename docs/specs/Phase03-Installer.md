# Phase 03 — Installer & Setup Wizard UX Specification

**Version:** 1.0.0  
**Date:** June 2026  
**Owner:** Senior UI/UX Designer + Senior DevOps Engineer  

---

## 1. Overview

The Setup Wizard is the first experience a new user has with the Fiji Enterprise Payroll System. It must be:
- Completable by a **non-technical payroll officer**
- Recoverable from any failure point
- Self-explanatory with inline help
- Idempotent (safe to re-run)

---

## 2. Wizard Framework

### 2.1 Window Specifications
- Title: `Fiji Enterprise Payroll — Setup Wizard`
- Size: 900×620px (fixed, non-resizable)
- Position: Centred on screen
- Non-closeable during critical operations (database creation, seed data)
- Always on top of other windows

### 2.2 Step Panel (Left Side — 220px)
Shows all 10 steps with visual status indicators:

| Icon | Meaning |
|------|---------|
| ⬜ | Not started |
| ▶ (blue) | Current step |
| ✅ (green) | Completed successfully |
| ❌ (red) | Failed |
| ⚠️ (amber) | Completed with warnings |

---

## 3. Step-by-Step Specification

---

### Step 1 — Database Connection

**Purpose:** Connect to an existing SQL Server instance.

**Fields:**
| Field | Type | Required | Default | Validation |
|-------|------|----------|---------|-----------|
| Server Name | Textbox | Yes | `(local)` | Not empty; valid hostname or IP |
| Instance Name | Textbox | No | — | Alphanumeric, backslash separator if named |
| Authentication Type | Radio | Yes | `SQL Server Authentication` | — |
| Username | Textbox | If SQL Auth | `sa` | Not empty if SQL auth |
| Password | Password box | If SQL Auth | — | Not empty if SQL auth |
| Database Name | Textbox | Yes | `FijiPayroll` | Letters, numbers, underscores only |
| Port | Textbox | No | `1433` | Number 1–65535 |

**Actions:**
- `[Test Connection]` button — immediately tests the connection
  - Success: Green checkmark + "Connection successful! SQL Server [version]"
  - Failure: Red X + error message + suggested fix
- `[Next]` — only enabled after successful connection test

**Connection test logic:**
```
1. Build connection string from form values
2. Attempt SqlConnection.Open() with 10-second timeout
3. If success: execute SELECT @@VERSION
4. Display result
5. Store validated connection string in wizard state
```

**Error Messages:**
| Scenario | Message | Suggested Fix |
|----------|---------|--------------|
| Server not found | "Could not reach server [Name]. Check server name and network." | Verify server name; check firewall |
| Auth failure | "Login failed for user [user]. Check username and password." | Verify credentials |
| Port blocked | "Connection timed out on port [port]." | Check firewall and SQL Server port |
| SQL Server version < 2019 | "SQL Server 2019 or higher required. Detected version: [X]." | Upgrade SQL Server |

---

### Step 2 — Database Creation

**Purpose:** Create the payroll database if it does not already exist.

**Display:**
- Database name (read-only, from Step 1)
- Status: "Database [Name] does not exist. It will be created." OR "Database [Name] already exists. Existing data will be preserved."

**Actions:**
- `[Create Database]` button
- Progress indicator (indeterminate spinner)

**Process:**
```
1. Check if database exists: SELECT DB_ID('[DatabaseName]')
2. If not exists: CREATE DATABASE [DatabaseName] 
   WITH initial size and auto-growth settings
3. Set compatibility level to 150
4. Set recovery model to FULL
5. Display success
```

**Recovery:**
- If database creation fails: Show error with option to retry
- If database exists and user proceeds: Validate schema version (for upgrades)

**Success message:** "Database [Name] created successfully on [Server]."

---

### Step 3 — Table Creation (Schema Migration)

**Purpose:** Create all database tables, views, indexes, and constraints.

**Display:**
- Progress bar (determinate — shows % complete)
- Live log: Each object as it is created
- Object count: "Creating object 47 of 312..."

**Process:**
```
1. Execute migration scripts in dependency order:
   a. Create schemas (system, company, employee, payroll, leave, audit, reporting)
   b. Create tables in dependency order (no FK violations)
   c. Create indexes
   d. Create views
   e. Create stored procedures
   f. Create triggers
2. Verify all objects created
3. Record schema version in system.SystemSettings
```

**Failure Handling:**
- On failure: Display which object failed and the SQL error
- Offer: [Retry Step] or [Rollback Database] (drops the database entirely)
- Rollback is only offered if this is a fresh installation (Step 2 created the DB)

**Success message:** "Schema created: [N] tables, [N] indexes, [N] views, [N] procedures."

---

### Step 4 — Seed Data

**Purpose:** Populate mandatory reference data.

**Display:**
- Checklist of seed data categories, each with a green checkmark as they complete:
  - ☐ Security Roles
  - ☐ Permissions
  - ☐ Role-Permission Assignments
  - ☐ Fiji Banks (BSP, ANZ, Westpac, HFC, Bred, Kontiki)
  - ☐ PAYE Tax Tables (2026)
  - ☐ FNPF Rates
  - ☐ Leave Types (Annual, Sick, Maternity, Paternity, Bereavement, Jury)
  - ☐ Notification Templates
  - ☐ System Settings (defaults)

**Process:**
- All seeds are idempotent (INSERT IF NOT EXISTS)
- Safe to re-run on upgrade

**Failure Handling:**
- Show which seed failed
- Allow retry of individual failed seed
- Skip non-critical seeds with warning

---

### Step 5 — Fiscal Calendar Generation

**Purpose:** Generate the fiscal calendar periods for the first year.

**Fields:**
| Field | Type | Required | Default | Validation |
|-------|------|----------|---------|-----------|
| Fiscal Year Start Month | Dropdown | Yes | January | 1–12 |
| Fiscal Year | Number | Yes | Current year | 4-digit year |
| Generate years ahead | Number | No | 1 | 1–5 |

**Preview:**
- Table showing the generated periods before confirmation
- Columns: Period #, Start Date, End Date, Description

**Process:**
```
1. User sets fiscal year start
2. System generates all periods (Jan–Dec or custom fiscal year)
3. Preview displayed
4. User confirms → periods written to company.FiscalCalendars
```

---

### Step 6 — Payroll Components

**Purpose:** Load default payroll component definitions.

**Display:**
- Checkbox list of default components to install:
  - ✅ Basic Salary (Earning)
  - ✅ PAYE Tax (Statutory Deduction)
  - ✅ FNPF Employee (Statutory Deduction)
  - ✅ FNPF Employer (Employer Contribution)
  - ✅ Overtime (Earning)
  - ✅ Housing Allowance (Allowance)
  - ✅ Transport Allowance (Allowance)
  - ✅ Meal Allowance (Allowance)
  - ✅ Annual Leave (Earning)
  - ✅ Sick Leave (Earning)

**Note:** Components marked `System` cannot be unchecked (they are mandatory).

---

### Step 7 — Leave Types

**Purpose:** Confirm standard Fiji leave types are installed.

**Display:**
Pre-populated table with:
| Leave Type | Days Per Year | Paid | Accrual Method |
|-----------|--------------|------|----------------|
| Annual Leave | 10 | Yes | Per period |
| Sick Leave | 10 | Yes | Annual grant |
| Maternity Leave | 84 days | Yes | Per event |
| Paternity Leave | 5 days | Yes | Per event |
| Bereavement Leave | 3 days | Yes | Per event |
| Jury Duty Leave | Unlimited | Yes | Per event |
| Unpaid Leave | Unlimited | No | Per event |

User can add custom leave types here. Minimum: Annual + Sick must be installed.

---

### Step 8 — Security Roles

**Purpose:** Install and review default security roles.

**Default Roles:**
| Role | Description |
|------|-------------|
| System Administrator | Full access, cannot be restricted |
| Payroll Administrator | Full payroll processing |
| HR Manager | Employee management, read-only payroll |
| Finance Manager | Reports, bank files, approval |
| Data Entry | Add/edit employees, no payroll processing |
| Auditor | Read-only across all modules |

**Display:** Role list with permission summary. User can click to preview permissions but cannot change at this stage (configurable post-installation in Settings).

---

### Step 9 — Administrator User

**Purpose:** Create the first System Administrator account.

**Fields:**
| Field | Type | Required | Validation |
|-------|------|----------|-----------|
| First Name | Textbox | Yes | Not empty |
| Last Name | Textbox | Yes | Not empty |
| Username | Textbox | Yes | 3–50 chars, alphanumeric + underscore, unique |
| Email | Textbox | No | Valid email format |
| Password | Password box | Yes | Min 8 chars, must include upper, lower, number, special |
| Confirm Password | Password box | Yes | Must match Password |

**Password Strength Indicator:**
Visual bar: Weak / Fair / Strong / Very Strong

**Rules:**
- Username cannot be `admin`, `administrator`, `sa` (reserved words)
- Password is bcrypt-hashed before storage
- Account is immediately assigned System Administrator role

---

### Step 10 — Finish

**Purpose:** Confirm installation is complete and provide next steps.

**Display:**
```
✅  Database Created
✅  Schema Installed  
✅  Reference Data Loaded
✅  Fiscal Calendar Generated
✅  Payroll Components Configured
✅  Leave Types Configured
✅  Security Roles Installed
✅  Administrator Account Created

🎉 Fiji Enterprise Payroll is ready to use!

Your next steps:
1. Add your company profile (Company → Company Details)
2. Set up departments and branches
3. Configure payroll frequencies
4. Add employees
5. Run your first payroll

[Open Application]   [View Getting Started Guide]
```

---

## 4. General Wizard UX Rules

### 4.1 Navigation
- `[Back]` always available (except Step 1)
- `[Next]` only enabled after step validation passes
- Steps cannot be skipped (must complete in order)
- Completed steps can be revisited via the left panel

### 4.2 Progress Indicators
| Operation | Indicator |
|-----------|-----------|
| Quick check (< 1 sec) | Inline spinner in button |
| Medium operation (1–10 sec) | Indeterminate progress bar |
| Long operation (> 10 sec) | Determinate progress bar + elapsed time |

### 4.3 Retry Logic
| Operation | Retry Policy |
|-----------|-------------|
| Connection test | Manual retry (button) |
| Database creation | 3 auto-retries with 2-second delay |
| Table creation | Manual retry per failed object |
| Seed data | 3 auto-retries, then manual |

### 4.4 Rollback Strategy
| Scenario | Rollback Action |
|----------|----------------|
| Step 3 fails (new DB) | DROP DATABASE [Name] |
| Step 3 fails (existing DB) | No rollback — show repair instructions |
| Step 4 fails | DELETE seeded rows for failed category |
| Step 9 fails | DELETE created user record |

### 4.5 Log File
All wizard actions written to:
```
%ProgramData%\FijiPayroll\Logs\setup-[YYYYMMDD-HHmmss].log
```

Displayed at finish screen with option to open.

---

*Document maintained by: Senior UI/UX Designer*  
*Last updated: June 2026*
