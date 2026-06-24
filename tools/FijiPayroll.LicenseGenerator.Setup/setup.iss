#define MyAppName "Fiji Enterprise Payroll License Generator"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Datec Fiji Pte Limited"
#define MyAppURL "https://www.datec.com.fj"
#define MyAppExeName "FijiPayroll.LicenseGenerator.exe"

[Setup]
AppId={{D1A235F8-1324-4B7E-874C-EA605B853245}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\Fiji Payroll License Generator
DefaultGroupName=Fiji Enterprise Payroll License Generator
DisableProgramGroupPage=yes
OutputDir=..\..\release
OutputBaseFilename=FEPS_LicenseGenerator_v1.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
MinVersion=10.0
SetupIconFile=feps_license_icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
Name: "{app}\keys"
Name: "{app}\templates"

[Files]
Source: "..\..\tools\FijiPayroll.LicenseGenerator\bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "feps_license_icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FijiPayroll.LicenseGenerator\setup_assets\config.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FijiPayroll.LicenseGenerator\setup_assets\keys\private_key.pem"; DestDir: "{app}\keys"; Flags: ignoreversion
Source: "..\FijiPayroll.LicenseGenerator\setup_assets\templates\license_template.xml"; DestDir: "{app}\templates"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\feps_license_icon.ico"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{app}\unins000.exe"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon; IconFilename: "{app}\feps_license_icon.ico"
