#define MyAppName      "PDF to JPG Converter"
#define MyAppVersion   "1.0"
#define MyAppPublisher "HMZ"
#define MyAppExeName   "PdfToJpgConverter.exe"
#define MySourceDir    "publish"
#define MyIconFile     "app.ico"
#define MySetupIcon    "app.ico"
#define MyBmpFile      "Logo-HLT-trans.png"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppId={{A3F2C8D1-4B6E-4F9A-8C2D-1E5F7A9B3C4D}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=C:\Temp\PdfToJpgInstaller
OutputBaseFilename=PdfToJpgConverter_Setup_v{#MyAppVersion}
SetupIconFile={#MySetupIcon}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Main executable
Source: "{#MySourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Native PDFium libraries
Source: "{#MySourceDir}\x64\pdfium.dll"; DestDir: "{app}\x64"; Flags: ignoreversion
Source: "{#MySourceDir}\x86\pdfium.dll"; DestDir: "{app}\x86"; Flags: ignoreversion
Source: "{#MySourceDir}\icudt.dll";       DestDir: "{app}";     Flags: ignoreversion

; Patagames managed assemblies
Source: "{#MySourceDir}\Patagames.Pdf.dll";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceDir}\Patagames.Pdf.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceDir}\Patagames.Pdf.Wpf.dll";    DestDir: "{app}"; Flags: ignoreversion

; All remaining runtime files (DLLs, json, etc.)
Source: "{#MySourceDir}\*.dll";  DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceDir}\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceDir}\*.pdb";  DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}";              Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}";    Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";        Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
