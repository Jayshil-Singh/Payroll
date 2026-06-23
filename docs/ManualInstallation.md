# Fiji Enterprise Payroll System — Technical Consultant Installation & Configuration Guide

**Version:** 1.0.0  
**Target Audience:** Technical Consultants, DevOps Engineers, System Administrators  
**Date:** June 2026  

---

## 1. System Requirements & Prerequisites

### 1.1 Client Machines (WPF Client App)
* **Operating System:** Windows 10 (Build 1809+) or Windows 11 (x64)
* **Runtime:** [.NET Desktop Runtime 9.0.x (x64)](https://dotnet.microsoft.com/download/dotnet/9.0)
* **Memory:** 8 GB RAM minimum (16 GB recommended)
* **Storage:** 500 MB free space

### 1.2 Local Database or Central Server
* **DBMS:** Microsoft SQL Server 2019+ (Express, LocalDB, Standard, or Enterprise)
* **Authentication:** Windows Authentication (Integrated Security) or SQL Server Authentication
* **Default Database Instance:** `(localdb)\mssqllocaldb` (default development/single-user instance)

---

## 2. Licensing Setup & Generation

The application requires a valid offline license file (`license.fplic`) signature-verified against an embedded RSA key.

### 2.1 Generating a Development License
To generate a custom license file for a pilot client, run the `FijiPayroll.LicenseGenerator` tool:

```powershell
# Syntax
dotnet tools\FijiPayroll.LicenseGenerator\bin\Release\net9.0\FijiPayroll.LicenseGenerator.dll `
    --company "<Client Company Name>" `
    --expiry "yyyy-MM-dd" `
    --features "*" `
    --output "<Target Directory>\license.fplic"
```

*Example:*
```powershell
dotnet tools\FijiPayroll.LicenseGenerator\bin\Release\net9.0\FijiPayroll.LicenseGenerator.dll `
    --company "Fiji Enterprise Payroll - Development" `
    --expiry 2027-06-23 `
    --features "*" `
    --output src\FijiPayroll.WPF\bin\Release\net9.0-windows\license.fplic
```

### 2.2 Placing the License
1. **Interactive Prompt:** If no license file is found on startup, the application displays a file selector dialog prompting the consultant or user to locate the `.fplic` file. Once selected, the system automatically copies it to the application root directory.
2. **Pre-deployment:** Consultants can pre-place the `license.fplic` file in the same directory as `FijiPayroll.WPF.exe` to bypass the startup prompt.

---

## 3. Database Deployment & Seeding

The application employs Entity Framework Core (EF Core) migrations and auto-seeding.

### 3.1 Local Development Bootstrapper (`install.ps1`)
For single-user local setups, run the elevated bootstrapper script from the repository root:
```powershell
# Open PowerShell as Administrator
Set-ExecutionPolicy Bypass -Scope Process
.\install.ps1
```
This script will:
1. Ensure SQL Server LocalDB is running.
2. Create the data directory under `~/FijiPayroll`.
3. Build the solution and run Entity Framework migrations.
4. Auto-generate the default development license.
5. Create a desktop shortcut.

### 3.2 Manual EF Core Command Line
For custom network database deployments, execute migrations manually:
```powershell
dotnet ef database update `
    --project src\FijiPayroll.Persistence\FijiPayroll.Persistence.csproj `
    --startup-project src\FijiPayroll.Persistence\FijiPayroll.Persistence.csproj `
    --connection "Server=my-sql-server;Database=FijiPayrollDb;User Id=sa;Password=myPassword;TrustServerCertificate=True;"
```

### 3.3 Auto-Migration & Seeding on Startup
If no manual migrations are executed, the WPF client app automatically runs `await context.Database.MigrateAsync()` on boot. It then triggers the seeders to populate initial metadata:
* **User Accounts:** Creates the default administrator account (Username: `admin`, Password: `P@ssw0rd123!`).
* **Tax Tables:** Seeds standard FRCS (Fiji Revenue and Customs Service) tax brackets.
* **FNPF Tables:** Seeds default FNPF (Fiji National Provident Fund) rates and employee/employer portions.
* **Master Lists:** Seeds default banks (ANZ, BSP, Westpac, Baroda) and pay cycles.

---

## 4. Configuration Settings (`appsettings.json`)

The client application looks for `appsettings.json` in the executable folder to resolve connection and server settings.

### 4.1 Connection & File Storage Settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql-server.domain;Database=FijiPayroll;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "FileStorage": {
    "RootDirectory": "\\\\network-share\\payroll\\exports\\"
  }
}
```

### 4.2 Application Config Options (SystemSettings Table)
Dynamic system settings can be configured inside the UI Dashboard (**Settings Menu**) or updated directly in the database:
* **Default Pay Frequency:** `Weekly`, `BiWeekly`, `SemiMonthly`, `Monthly`
* **Negative Pay Policy:** Determines behavior when deductions exceed gross earnings (`AllowNegative`, `ClampToZero`, `Error`).
* **Default Backup Directory:** Destination for backup files.
* **SMTP Settings:** Host, port, username, password, and SSL flag for payroll payslip delivery.

---

## 5. Security & PII Encryption

1. **AES-256 PII Protection:** The database stores sensitive PII data (TIN, FNPF numbers, birthdates) encrypted. Plaintext records are migrated to AES-256 automatically at first run via `context.MigratePlaintextToAesAsync()`.
2. **Password Security:** Multi-tenant password history is enforced. The default admin password (`P@ssw0rd123!`) must be changed immediately upon first login.
3. **Audit Log:** Every compliance-affecting change (approving a payroll run, changing tax settings, overriding locks) generates a structured record in the `AuditLogs` table.

---

## 6. Logs & Diagnostics

All application events, database statements, and service heartbeat statuses are written to the dynamic memory log buffer:
* **In-Memory Buffer:** Captured by the presentation monitor.
* **Event Log/Text Files:** Stored in the application run directory under `/logs` folder or system diagnostics depending on environment config.
* **Health Watchdogs:** The in-process watchdogs (`SystemHealthMonitor`, `SystemIntegrityValidator`) monitor memory allocation leaks and session activity continuously.

---
*Document maintained by: Technical Services & Deployment Team*  
*Last updated: June 2026*
