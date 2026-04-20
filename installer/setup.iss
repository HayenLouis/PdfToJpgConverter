#define MyAppName      "PDF to JPG Converter"
#define MyAppVersion   "1.2"
#define MyAppPublisher "HMZ"
#define MyAppExeName   "PdfToJpgConverter.exe"
#define MySourceDir    "..\publish"
#define MyIconFile     "..\assets\app.ico"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppId={{A3F2C8D1-4B6E-4F9A-8C2D-1E5F7A9B3C4D}
DefaultDirName={localappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=.
OutputBaseFilename=PdfToJpgConverter_Setup_v{#MyAppVersion}
SetupIconFile={#MyIconFile}
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
Source: "{#MySourceDir}\{#MyAppExeName}";    DestDir: "{app}"; Flags: ignoreversion

; App dependencies
Source: "{#MySourceDir}\*.dll";              DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceDir}\*.json";             DestDir: "{app}"; Flags: ignoreversion

; Native PDFium libraries
Source: "{#MySourceDir}\x64\pdfium.dll";     DestDir: "{app}\x64"; Flags: ignoreversion
Source: "{#MySourceDir}\x86\pdfium.dll";     DestDir: "{app}\x86"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}";             Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}";   Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";       Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  ErrorCode: Integer;

function IsDotNet8DesktopInstalled(): Boolean;
var
  RegKey: String;
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  RegKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  if RegGetSubkeyNames(HKLM, RegKey, Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
      if Copy(Names[I], 1, 2) = '8.' then
      begin
        Result := True;
        Exit;
      end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet8DesktopInstalled() then
  begin
    MsgBox(
      '.NET 8 Desktop Runtime is not installed.' + #13#10#13#10 +
      'PDF to JPG Converter requires it to run.' + #13#10 +
      'After clicking OK, your browser will open the Microsoft download page.' + #13#10#13#10 +
      'Download and install the ".NET Desktop Runtime 8.x (x64)" and then re-run this installer.',
      mbInformation, MB_OK);
    ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, ErrorCode);
    Result := False;
  end;
end;
