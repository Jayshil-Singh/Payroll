# Fiji Enterprise Payroll System Setup Builder Script
$ErrorActionPreference = "Stop"

Write-Host "======================================================================"
Write-Host "      Fiji Enterprise Payroll System - Installer Build Automation"
Write-Host "======================================================================"
Write-Host ""

$ScriptRoot = $PSScriptRoot
$ProjectRoot = (Get-Item $ScriptRoot).Parent.FullName
$WpfProj = "$ProjectRoot\src\FijiPayroll.WPF\FijiPayroll.WPF.csproj"
$LicGenProj = "$ProjectRoot\tools\FijiPayroll.LicenseGenerator\FijiPayroll.LicenseGenerator.csproj"
$WpfSetupIss = "$ScriptRoot\FijiPayroll.Setup\setup.iss"
$LicGenSetupIss = "$ScriptRoot\FijiPayroll.LicenseGenerator.Setup\setup.iss"
$IsccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# 1. Verify Compiler
if (-not (Test-Path $IsccPath)) {
    Write-Error "ISCC compiler not found at: $IsccPath"
}

# 2. Publish WPF
Write-Host "--> Publishing FijiPayroll.WPF..."
$WpfPublishDir = "$ProjectRoot\src\FijiPayroll.WPF\bin\Release\net9.0-windows\win-x64\publish"
& dotnet publish $WpfProj -c Release -r win-x64 --self-contained true -o $WpfPublishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish WPF"
}
Write-Host "WPF published successfully."

# 3. Publish LicenseGenerator
Write-Host "--> Publishing FijiPayroll.LicenseGenerator..."
$LicGenPublishDir = "$ProjectRoot\tools\FijiPayroll.LicenseGenerator\bin\Release\net9.0\win-x64\publish"
& dotnet publish $LicGenProj -c Release -r win-x64 --self-contained true -o $LicGenPublishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish LicenseGenerator"
}
Write-Host "LicenseGenerator published successfully."

# 4. Compile Customer Setup
Write-Host "--> Compiling Customer Setup Installer..."
& $IsccPath $WpfSetupIss
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to compile Customer Setup"
}

# 5. Compile License Generator Setup
Write-Host "--> Compiling License Generator Setup Installer..."
& $IsccPath $LicGenSetupIss
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to compile License Generator Setup"
}

# Summary
$ReleaseDir = "$ProjectRoot\release"
Write-Host "======================================================================"
Write-Host "  BUILD COMPLETE"
Write-Host "======================================================================"
Write-Host "Generated Installers located in: $ReleaseDir"
Get-ChildItem -Path $ReleaseDir -Filter "*.exe" | Select-Object Name, Length, LastWriteTime
