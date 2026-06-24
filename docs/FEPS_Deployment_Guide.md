# Fiji Enterprise Payroll System (FEPS) — DevOps & Deployment Guide

**Version:** 1.0  
**Target Audience:** Release Engineers, DevOps Specialists, Implementation Consultants  

---

## 1. Directory Anatomy
The installer allocates and permissions the following directories during deployment:

* **Binaries Folder (`C:\Program Files\Fiji Payroll\`)**
  Holds the self-contained WPF application, supporting assemblies, configuration files, and empty `Templates` and `Reports` placeholders.
* **System Data Folder (`C:\ProgramData\Fiji Payroll\`)**
  Holds databases, security descriptors, and exports.
  * `FijiPayrollDb.mdf` (Database file)
  * `Logs\` (Active logs including `install.log`, `bootstrap.log`, and application logs)
  * `License\license.fplic` (Customer offline license)
  * `Backups\` (Database database backup dumps)
  * `Exports\` (Compliance FNPF & FRCS file exports)

---

## 2. Release & Build Automation
To rebuild the installers, follow these steps on a build machine containing **Inno Setup 6**:

1. Open PowerShell.
2. Run the build script:
   ```powershell
   cd tools
   .\build_installers.ps1
   ```
3. The build automation script executes:
   * Self-contained Release publishes:
     `dotnet publish -c Release -r win-x64 --self-contained true`
   * Compiles the customer setup script:
     `ISCC.exe FijiPayroll.Setup\setup.iss`
   * Compiles the license generator setup script:
     `ISCC.exe FijiPayroll.LicenseGenerator.Setup\setup.iss`
4. The outputs are placed in the `release/` folder under the workspace root.

---

## 3. Cryptographic Key Management & Security
To satisfy strict security policies, the cryptographic keys are strictly partitioned:

### 3.1 Public Key (Client App)
* Embedded inside the client WPF assemblies (`LicenseValidator.cs`).
* Can optionally be overridden by placing a `public_key.pem` in `C:\Program Files\Fiji Payroll\`.
* Only verify functions are packaged with the customer installer.

### 3.2 Private Key (Internal Consultant Tool)
* The private signing key corresponds to the default signature checker.
* Embedded inside `FijiPayroll.LicenseGenerator.exe`.
* Deployed as a key file (`keys/private_key.pem`) ONLY inside the `FijiPayroll.LicenseGenerator.Setup` installer.
* **WARNING:** The License Generator installer must never be distributed to customers.

---

## 4. In-Place Upgrades
The installer supports smooth upgrades without data loss:
* **App settings Preservation:** `appsettings.json` is marked with `Flags: onlyifdoesntexist` which prevents overwriting customized connection strings.
* **Data Preservation:** Customer databases, FNPF/FRCS exports, backups, and license files reside in `C:\ProgramData\Fiji Payroll` and are kept intact.
* **EF Auto-migration:** When the upgrade installer runs, it calls `FijiPayroll.WPF.exe --migrate` which upgrades the existing database schema to the latest version while retaining all client records.
