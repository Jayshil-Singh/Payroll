#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Fiji Enterprise Payroll System — Deployment Bootstrapper v1.0

.DESCRIPTION
    Provisions SQL Server LocalDB, applies EF Core migrations, seeds initial data,
    generates a development license, and creates desktop shortcuts.

    This script is intended for first-time deployment or migration upgrades.

.NOTES
    Run this script from the repository root directory.
    Requires elevated privileges (Administrator).

.EXAMPLE
    .\install.ps1
    .\install.ps1 -SkipLicense
    .\install.ps1 -ConnectionString "Server=.\SQLEXPRESS;Database=FijiPayrollDb;Trusted_Connection=True;"
#>

[CmdletBinding()]
param(
    [Parameter(HelpMessage = "Override the default SQL Server connection string.")]
    [string]$ConnectionString = "",

    [Parameter(HelpMessage = "Skip generating the development license file.")]
    [switch]$SkipLicense,

    [Parameter(HelpMessage = "Skip creating desktop shortcuts.")]
    [switch]$SkipShortcuts,

    [Parameter(HelpMessage = "Company name for the development license.")]
    [string]$LicenseCompany = "Fiji Enterprise Payroll - Development",

    [Parameter(HelpMessage = "License expiry date (yyyy-MM-dd). Defaults to 1 year from today.")]
    [string]$LicenseExpiry = ""
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ─── Console Helpers ─────────────────────────────────────────────────────────

function Write-Banner {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║    Fiji Enterprise Payroll System — Deployment Bootstrapper  ║" -ForegroundColor Cyan
    Write-Host "║                        Version 1.0                          ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Number, [string]$Title)
    Write-Host ""
    Write-Host "── Step $Number : $Title ──────────────────────────────────" -ForegroundColor Yellow
}

function Write-Ok {
    param([string]$Message)
    Write-Host "  ✔ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "  ℹ $Message" -ForegroundColor Gray
}

function Write-Warn {
    param([string]$Message)
    Write-Host "  ⚠ $Message" -ForegroundColor DarkYellow
}

function Write-Fail {
    param([string]$Message)
    Write-Host "  ✘ $Message" -ForegroundColor Red
}

# ─── Main ────────────────────────────────────────────────────────────────────

Write-Banner

$ScriptRoot = $PSScriptRoot
$SrcRoot = Join-Path $ScriptRoot "src"
$ToolsRoot = Join-Path $ScriptRoot "tools"
$WpfProject = Join-Path $SrcRoot "FijiPayroll.WPF" "FijiPayroll.WPF.csproj"
$PersistenceProject = Join-Path $SrcRoot "FijiPayroll.Persistence" "FijiPayroll.Persistence.csproj"
$LicenseGenProject = Join-Path $ToolsRoot "FijiPayroll.LicenseGenerator" "FijiPayroll.LicenseGenerator.csproj"

$UserProfile = [Environment]::GetFolderPath("UserProfile")
$DbDir = Join-Path $UserProfile "FijiPayroll"

# ── Step 1: Validate Prerequisites ───────────────────────────────────────────

Write-Step "1" "Validating Prerequisites"

# Check .NET SDK
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Ok "Found .NET SDK: $dotnetVersion"
} catch {
    Write-Fail ".NET SDK is not installed or not in PATH."
    Write-Host "  Download from: https://dotnet.microsoft.com/download" -ForegroundColor Gray
    exit 1
}

# Check EF Core tools
try {
    $efVersion = & dotnet ef --version 2>&1
    if ($LASTEXITCODE -ne 0) { throw "EF tools not found" }
    Write-Ok "Found EF Core tools: $efVersion"
} catch {
    Write-Warn "EF Core tools not found. Installing..."
    & dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "Failed to install EF Core tools."
        exit 1
    }
    Write-Ok "EF Core tools installed."
}

# Check SQL Server LocalDB
try {
    $sqllocaldb = Get-Command sqllocaldb -ErrorAction Stop
    $instances = & sqllocaldb info 2>&1
    Write-Ok "SQL Server LocalDB is available."
    Write-Info "Existing instances: $($instances -join ', ')"
} catch {
    Write-Warn "SQL Server LocalDB not detected. Ensure SQL Server LocalDB is installed."
    Write-Info "The application may still work if a SQL Server instance is available on the network."
}

# ── Step 2: Create Database Directory ────────────────────────────────────────

Write-Step "2" "Provisioning Database Directory"

if (-not (Test-Path $DbDir)) {
    New-Item -ItemType Directory -Path $DbDir -Force | Out-Null
    Write-Ok "Created database directory: $DbDir"
} else {
    Write-Ok "Database directory already exists: $DbDir"
}

# Ensure LocalDB MSSQLLocalDB instance is running
try {
    $instanceInfo = & sqllocaldb info MSSQLLocalDB 2>&1
    if ($instanceInfo -match "State.*Running") {
        Write-Ok "LocalDB MSSQLLocalDB instance is running."
    } else {
        Write-Info "Starting LocalDB MSSQLLocalDB instance..."
        & sqllocaldb start MSSQLLocalDB 2>&1 | Out-Null
        Write-Ok "LocalDB MSSQLLocalDB instance started."
    }
} catch {
    Write-Warn "Could not manage LocalDB instance. Database migration may still succeed if an instance is available."
}

# ── Step 3: Build Solution ───────────────────────────────────────────────────

Write-Step "3" "Building Solution"

Write-Info "Restoring NuGet packages..."
& dotnet restore $WpfProject --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Fail "NuGet restore failed."
    exit 1
}
Write-Ok "NuGet packages restored."

Write-Info "Building WPF application..."
& dotnet build $WpfProject --configuration Release --verbosity quiet --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Fail "Build failed. Check for compilation errors above."
    exit 1
}
Write-Ok "Build successful."

# ── Step 4: Apply Database Migrations ────────────────────────────────────────

Write-Step "4" "Applying Database Migrations"

$MdfPath = Join-Path $DbDir "FijiPayrollDb.mdf"
if ([string]::IsNullOrEmpty($ConnectionString)) {
    $ConnectionString = "Server=(localdb)\mssqllocaldb;Database=FijiPayrollDb;Trusted_Connection=True;MultipleActiveResultSets=true;AttachDbFileName=$MdfPath"
}

Write-Info "Connection string: $($ConnectionString.Substring(0, [Math]::Min(80, $ConnectionString.Length)))..."

try {
    & dotnet ef database update `
        --project $PersistenceProject `
        --startup-project $WpfProject `
        --configuration Release `
        --verbose 2>&1 | Where-Object { $_ -match "Applying migration|Done\." }

    if ($LASTEXITCODE -ne 0) {
        Write-Warn "EF migration command returned non-zero exit code. This may be benign if migrations are already applied."
    } else {
        Write-Ok "Database migrations applied successfully."
    }
} catch {
    Write-Warn "Migration command encountered an error: $_"
    Write-Info "The application will attempt to apply migrations on first launch."
}

# ── Step 5: Generate Development License ─────────────────────────────────────

Write-Step "5" "Generating Development License"

if ($SkipLicense) {
    Write-Info "License generation skipped (--SkipLicense flag)."
} else {
    if ([string]::IsNullOrEmpty($LicenseExpiry)) {
        $LicenseExpiry = (Get-Date).AddYears(1).ToString("yyyy-MM-dd")
    }

    # Build the license generator
    Write-Info "Building LicenseGenerator tool..."
    & dotnet build $LicenseGenProject --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Warn "Failed to build LicenseGenerator. Skipping license generation."
    } else {
        $LicenseGenDll = Join-Path $ToolsRoot "FijiPayroll.LicenseGenerator" "bin" "Release" "net9.0" "FijiPayroll.LicenseGenerator.dll"

        # Determine output location (next to WPF binary)
        $WpfBinDir = Join-Path $SrcRoot "FijiPayroll.WPF" "bin" "Release" "net9.0-windows"
        if (-not (Test-Path $WpfBinDir)) {
            $WpfBinDir = Join-Path $SrcRoot "FijiPayroll.WPF" "bin" "Debug" "net9.0-windows"
        }

        $LicenseOutputPath = Join-Path $WpfBinDir "license.fplic"

        Write-Info "Generating license for '$LicenseCompany' (expires $LicenseExpiry)..."
        & dotnet $LicenseGenDll --company $LicenseCompany --expiry $LicenseExpiry --features "*" --output $LicenseOutputPath

        if ($LASTEXITCODE -eq 0) {
            Write-Ok "Development license generated: $LicenseOutputPath"
        } else {
            Write-Warn "License generation failed. You can manually run the LicenseGenerator tool later."
        }
    }
}

# ── Step 6: Create Desktop Shortcut ──────────────────────────────────────────

Write-Step "6" "Creating Desktop Shortcut"

if ($SkipShortcuts) {
    Write-Info "Shortcut creation skipped (--SkipShortcuts flag)."
} else {
    try {
        $WpfExe = Join-Path $SrcRoot "FijiPayroll.WPF" "bin" "Release" "net9.0-windows" "FijiPayroll.WPF.exe"
        if (-not (Test-Path $WpfExe)) {
            $WpfExe = Join-Path $SrcRoot "FijiPayroll.WPF" "bin" "Debug" "net9.0-windows" "FijiPayroll.WPF.exe"
        }

        if (Test-Path $WpfExe) {
            $DesktopPath = [Environment]::GetFolderPath("Desktop")
            $ShortcutPath = Join-Path $DesktopPath "Fiji Enterprise Payroll.lnk"

            $WshShell = New-Object -ComObject WScript.Shell
            $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
            $Shortcut.TargetPath = $WpfExe
            $Shortcut.WorkingDirectory = Split-Path $WpfExe
            $Shortcut.Description = "Fiji Enterprise Payroll System"
            $Shortcut.Save()

            [System.Runtime.Interopservices.Marshal]::ReleaseComObject($WshShell) | Out-Null

            Write-Ok "Desktop shortcut created: $ShortcutPath"
        } else {
            Write-Warn "WPF executable not found. Shortcut was not created."
            Write-Info "Build the application first, then re-run this script."
        }
    } catch {
        Write-Warn "Could not create desktop shortcut: $_"
    }
}

# ── Summary ──────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Deployment Complete" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Database Dir:    $DbDir" -ForegroundColor White
Write-Host "  License:         $(if ($SkipLicense) { 'Skipped' } else { $LicenseOutputPath })" -ForegroundColor White
Write-Host "  Next Steps:" -ForegroundColor White
Write-Host "    1. Launch the application from the desktop shortcut or:" -ForegroundColor Gray
Write-Host "       dotnet run --project $WpfProject" -ForegroundColor DarkGray
Write-Host "    2. Log in with default credentials: admin / P@ssw0rd123!" -ForegroundColor Gray
Write-Host "    3. You will be prompted to change the password on first login." -ForegroundColor Gray
Write-Host ""
