# Fiji Enterprise Payroll System (FEPS) — Setup & Installation Guide

**Version:** 1.0  
**Target Audience:** Customers, Technical Consultants, System Administrators  
**Setup File:** `FEPS_Setup_v1.0.exe`  

---

## 1. Prerequisites
Before launching the setup, ensure the client machine satisfies the following prerequisites:
* **Operating System:** Windows 10 (Build 1809+) or Windows 11 (64-bit).
* **Database engine:** SQL Server LocalDB (MSSQLLocalDB instance) or a network-reachable Microsoft SQL Server 2019+ instance.
* **Privileges:** Administrator privileges are required to run the installer and configure Program Files/ProgramData folders.
* **Disk Space:** Minimum 200 MB free space.

---

## 2. Walkthrough: Installing FEPS
Follow these graphical wizard steps to deploy the payroll system:

### Step 2.1: Run the Setup File
1. Locate and double-click `FEPS_Setup_v1.0.exe`.
2. Windows User Account Control (UAC) will prompt you for administrator access. Click **Yes**.

### Step 2.2: Choose Installation Folder
* The default path is `C:\Program Files\Fiji Payroll\`. Click **Next**.

### Step 2.3: License Import (Optional)
* The installer presents a custom **Import License File** dialog.
* Click **Browse** and select your signature-verified offline license file (`license.fplic`).
* *Note:* If you do not have a license yet, you can leave it blank and import it later upon application startup. Click **Next**.

### Step 2.4: Shortcut Options
* Select whether to create a Desktop shortcut or a Start Menu program folder. Click **Next**.

### Step 2.5: Install and Auto-Bootstrap
* Click **Install**. The wizard will:
  1. Extract and copy application binaries to `C:\Program Files\Fiji Payroll\`.
  2. Create configuration directories under `C:\ProgramData\Fiji Payroll\`.
  3. Copy your license file (if imported) to `C:\ProgramData\Fiji Payroll\License\license.fplic`.
  4. Silently execute the database bootstrapper to create and migrate the database schema and seed default data.
* Once the progress bar fills, click **Finish**.

---

## 3. Post-Installation Steps

### 3.1 Initial Login
1. Launch the application from the desktop shortcut or Start Menu.
2. Enter the default administrator credentials:
   * **Username:** `admin`
   * **Password:** `ChangeMe123!`
3. The system will prompt you to change the temporary password immediately. Choose a strong compliance-friendly password.

### 3.2 Dynamic Configuration
If you need to connect to a centralized SQL Server instead of the default local instance:
1. Open `C:\Program Files\Fiji Payroll\appsettings.json` with a text editor.
2. Modify the `ConnectionStrings:DefaultConnection` entry to your remote SQL Server instance connection string.
3. Save the file (requires administrator privileges).
