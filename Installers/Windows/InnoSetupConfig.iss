; Script Inno Setup Script Wizard, pour le multi-architecture.
; Hofer Lukas
; 04.06.2026

#define MyAppName "ScholarLog"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Hofer Lukas"
#define MyAppURL "https://github.com/Hoferlukaslh/ScholarLog"
#define MyAppExeName "ScholarLog.exe"
#define SetupLogo "C:\Users\lukas\Desktop\scholarLog\src\Assets\Images\ScholarLogLogoInstaller.ico"
#define BuildPath "C:\Users\lukas\Desktop\Build"

[Setup]
AppId={{4E58FF3A-1644-48FF-B6DB-6CF81441192A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

ArchitecturesAllowed=x86 x64os arm64
ArchitecturesInstallIn64BitMode=x64os arm64

DisableProgramGroupPage=yes
LicenseFile=C:\Users\lukas\Desktop\scholarLog\COPYING.md
PrivilegesRequiredOverridesAllowed=dialog

OutputBaseFilename=ScholarLog_Installer_WIN_Universal
SolidCompression=yes
WizardStyle=modern windows11

; Icone ScholarLog
SetupIconFile={#SetupLogo}

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Fichiers spécifiques pour Windows x86 (32-bit)
Source: "{#BuildPath}\WINx86\{#MyAppExeName}"; DestDir: "{app}"; Check: IsX86; Flags: ignoreversion

; Fichiers spécifiques pour Windows x64 (Remplacement de IsX64 par IsX64OS)
Source: "{#BuildPath}\WINx64\{#MyAppExeName}"; DestDir: "{app}"; Check: IsX64OS; Flags: ignoreversion

; Fichiers spécifiques pour Windows ARM64
Source: "{#BuildPath}\WIN_ARM64\{#MyAppExeName}"; DestDir: "{app}"; Check: IsArm64; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent