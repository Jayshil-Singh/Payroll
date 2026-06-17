# Fiji Enterprise Payroll System — Deployment & Installation Guide

**Version:** 1.0.0  
**Date:** June 2026  
**Status:** Approved  
**Owner:** Jayshil Singh

---

## 1. Prerequisites

Before installing the application, ensure the host environments meet the following requirements:

### Client Machine (WPF Client App)
* **Operating System:** Windows 10 (Build 1809+) or Windows 11.
* **Runtime:** [.NET Desktop Runtime 9.0.x](https://dotnet.microsoft.com/download/dotnet/9.0) (x64).
* **Memory:** 8 GB RAM minimum.
* **Storage:** 500 MB free space.

### Database Server
* **DBMS:** Microsoft SQL Server 2019+ (Express, Standard, or Enterprise).
* **Authentication:** Windows Authentication (Integrated Security) or SQL Server Authentication.

---

## 2. Database Deployment

1. Run the database migration bundle executable or use Entity Framework Core tools to create the schema:
   ```powershell
   dotnet ef database update --project FijiPayroll.Persistence --startup-project FijiPayroll.WPF
   ```
2. Ensure the startup bootstrapper seeds the initial parameters (Roles, Bank Masters, Default Tax Tables, and FNPF Rates).

---

## 3. Client Installation (WPF Desktop App)

The application is deployed using standard Windows Installer (`.msi`) packages generated via the Setup project.

### 3.1 Step-by-Step Installation
1. Double-click the `FijiPayroll.Setup.msi` installer.
2. Accept the licensing terms.
3. Select the installation path (default: `C:\Program Files (x86)\Fiji Enterprise Payroll\`).
4. Click **Install**.
5. Once complete, a desktop shortcut and Start Menu folder will be created.

### 3.2 Configuration Files (`appsettings.json`)
The client app requires connection details to the centralized database and the file storage location.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver.local;Database=FijiPayroll;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "FileStorage": {
    "RootDirectory": "\\\\fileserver\\payroll-exports\\"
  }
}
```

---

## 4. Background Channels Processing Setup

No dedicated background service installation is necessary for standard clients. The WPF client launches the `ComplianceJobProcessor` and `BackgroundScheduler` in-process using background channels on startup:

```
[WPF Shell Startup]
   │
   ├── Initializes State & Dependency Injection
   ├── Starts BackgroundScheduler
   └── Starts ComplianceJobProcessor channel worker
```

---

*Document maintained by: DevOps Engineer*  
*Last updated: June 2026*
