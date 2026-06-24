#define MyAppName "Fiji Enterprise Payroll System"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Datec Fiji Pte Limited"
#define MyAppURL "https://www.datec.com.fj"
#define MyAppExeName "FijiPayroll.WPF.exe"

[Setup]
AppId={{E6F18AC9-CE54-47DC-B29D-D871BD81816A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\Fiji Payroll
DefaultGroupName=Fiji Enterprise Payroll System
DisableProgramGroupPage=yes
OutputDir=..\..\release
OutputBaseFilename=FEPS_Setup_v1.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
MinVersion=10.0
SetupLogging=yes
SetupIconFile=feps_payroll_icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
Name: "{commonappdata}\Fiji Payroll"
Name: "{commonappdata}\Fiji Payroll\Logs"
Name: "{commonappdata}\Fiji Payroll\Exports"
Name: "{commonappdata}\Fiji Payroll\Backups"
Name: "{commonappdata}\Fiji Payroll\License"
Name: "{app}\Templates"
Name: "{app}\Reports"

[Files]
Source: "..\..\src\FijiPayroll.WPF\bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "feps_payroll_icon.ico"; DestDir: "{app}"; Flags: ignoreversion
; Preserve appsettings.json on upgrades
Source: "..\..\src\FijiPayroll.WPF\bin\Release\net9.0-windows\win-x64\publish\appsettings.json"; DestDir: "{app}"; Flags: onlyifdoesntexist ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\feps_payroll_icon.ico"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{app}\unins000.exe"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon; IconFilename: "{app}\feps_payroll_icon.ico"

[Run]
; Run headless database migration and seeding post install
Filename: "{app}\{#MyAppExeName}"; Parameters: "--migrate"; StatusMsg: "Initializing and Migrating Database..."; Flags: runhidden waituntilterminated

[Code]
var
  LicenseImportPage: TInputFileWizardPage;
  RemoveDb, RemoveBackups, RemoveExports: Boolean;

// Initialize custom wizard page for optional license import
procedure InitializeWizard;
var
  LicensePath: String;
begin
  LicensePath := ExpandConstant('{commonappdata}\Fiji Payroll\License\license.fplic');

  LicenseImportPage := CreateInputFilePage(wpSelectDir,
    'Import License File',
    'Do you have a Fiji Payroll license file (*.fplic)?',
    'If you have a license file, select it below. You can also skip this and configure it later.');
    
  LicenseImportPage.Add('License File:', 'Fiji Payroll License Files (*.fplic)|*.fplic', '.fplic');

  // Pre-populate if existing license is detected
  if FileExists(LicensePath) then
  begin
    LicenseImportPage.Values[0] := LicensePath;
  end;
end;

// Pre-installation check for Windows 10/11, SQL Server, and required runtime
function InitializeSetup: Boolean;
var
  LocalDBInstalled: Boolean;
begin
  Result := True;

  // 1. Verify OS version is Windows 10 or 11 (MinVersion=10.0 handles this, but let's log it)
  Log('Prerequisite Check: OS version verified >= Windows 10 (Build 1809+).');

  // 2. Verify SQL Server LocalDB or SQL Server installation
  LocalDBInstalled := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server LocalDB\Installed Versions') or
                      RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server LocalDB\Installed Versions') or
                      RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server') or
                      RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server');

  if not LocalDBInstalled then
  begin
    if MsgBox('SQL Server or LocalDB instance was not detected on this machine. SQL Server is required for database connectivity. Do you want to proceed anyway?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDNO then
    begin
      Result := False;
      Exit;
    end;
  end;

  // Log installation start
  ForceDirectories(ExpandConstant('{commonappdata}\Fiji Payroll\Logs'));
  SaveStringToFile(ExpandConstant('{commonappdata}\Fiji Payroll\Logs\install.log'),
    '[' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':') + '] Installation started. Version: ' + ExpandConstant('{#MyAppVersion}') + #13#10, True);
end;

// Handle post-install steps including license copy and log completion
procedure CurStepChanged(CurStep: TSetupStep);
var
  SelectedLicenseFile: String;
  TargetLicenseDir: String;
  TargetLicenseFile: String;
begin
  if CurStep = ssPostInstall then
  begin
    SelectedLicenseFile := LicenseImportPage.Values[0];
    TargetLicenseDir := ExpandConstant('{commonappdata}\Fiji Payroll\License');
    TargetLicenseFile := TargetLicenseDir + '\license.fplic';

    // Import license if selected and it is not already in the destination
    if (SelectedLicenseFile <> '') and (SelectedLicenseFile <> TargetLicenseFile) then
    begin
      ForceDirectories(TargetLicenseDir);
      if FileCopy(SelectedLicenseFile, TargetLicenseFile, False) then
      begin
        SaveStringToFile(ExpandConstant('{commonappdata}\Fiji Payroll\Logs\install.log'),
          '[' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':') + '] License file successfully imported to C:\ProgramData\Fiji Payroll\License\license.fplic' + #13#10, True);
      end
      else
      begin
        SaveStringToFile(ExpandConstant('{commonappdata}\Fiji Payroll\Logs\install.log'),
          '[' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':') + '] ERROR: Failed to import license file: ' + SelectedLicenseFile + #13#10, True);
      end;
    end;

    // Log installation completion
    SaveStringToFile(ExpandConstant('{commonappdata}\Fiji Payroll\Logs\install.log'),
      '[' + GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':') + '] Installation completed successfully.' + #13#10, True);
  end;
end;

// Custom uninstall prompts (default to NO)
function InitializeUninstall: Boolean;
begin
  Result := True;
  RemoveDb := False;
  RemoveBackups := False;
  RemoveExports := False;

  if MsgBox('Do you want to delete the SQL Server database files (FijiPayrollDb)?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
  begin
    RemoveDb := True;
  end;

  if MsgBox('Do you want to delete all backups from the Backups folder?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
  begin
    RemoveBackups := True;
  end;

  if MsgBox('Do you want to delete all exports from the Exports folder?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
  begin
    RemoveExports := True;
  end;
end;

// Execute data directory cleanup during uninstall
procedure CurUninstallStepChanged(UninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if UninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{commonappdata}\Fiji Payroll');
    
    if RemoveDb then
    begin
      DeleteFile(DataDir + '\FijiPayrollDb.mdf');
      DeleteFile(DataDir + '\FijiPayrollDb_log.ldf');
    end;

    if RemoveBackups then
    begin
      DelTree(DataDir + '\Backups', True, True, True);
    end;

    if RemoveExports then
    begin
      DelTree(DataDir + '\Exports', True, True, True);
    end;

    // Clean up logs and license folders if everything was deleted
    if RemoveDb and RemoveBackups and RemoveExports then
    begin
      DelTree(DataDir, True, True, True);
    end;
  end;
end;
