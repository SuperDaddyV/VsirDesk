; LumiDesk Inno Setup installer script
; Build the app first, then compile this script with Inno Setup.
;
; Recommended build command:
;   dotnet publish LumiDesk\LumiDesk.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\win-x64
; If you use the recommended command, change MyAppSourceDir below to "publish\win-x64".

#define MyAppName "LumiDesk"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "LumiDesk"
#define MyAppURL "https://www.example.com/"
#define MyAppExeName "LumiDesk.exe"
#define MyAppSourceDir "LumiDesk\bin\Release\net9.0-windows10.0.18362.0"
#define MyAppIconSourceDir "LumiDesk\icon"
#define MyAppIconName "lumidesk1-removebg-preview.ico"
#define MyAppMutex "LumiDesk_SingleInstance_Mutex_6B9BD6F1-8E3A-4C5D-9F2B-1A7C8D3E5F9A"

[Setup]
; AppId uniquely identifies this application. Do not reuse it for other apps.
AppId={{4B0F3B03-7F5D-4B5D-B2F4-6816B931C7D2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=installer
OutputBaseFilename={#MyAppName}_Setup_{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=no
CloseApplications=yes
RestartApplications=no
AppMutex={#MyAppMutex}
SetupIconFile={#MyAppIconSourceDir}\{#MyAppIconName}
UninstallDisplayIcon={app}\icon\{#MyAppIconName}

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[CustomMessages]
chinesesimp.LaunchProgram=启动 %1

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "开机自动启动"; GroupDescription: "启动选项"; Flags: unchecked

[Dirs]
; LumiDesk stores user data in LocalAppData. Keep these directories after uninstall by default.
Name: "{localappdata}\LumiDesk"
Name: "{localappdata}\LumiDesk\icons"
Name: "{localappdata}\LumiDesk\logs"
Name: "{localappdata}\LumiDesk\cache"

[Files]
; Package the release output, including dlls, runtimeconfig, deps and native runtimes.
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Excludes: "icon\*"; Flags: ignoreversion recursesubdirs createallsubdirs
; Package the complete icon assets from the project source directory.
Source: "{#MyAppIconSourceDir}\*"; DestDir: "{app}\icon"; Flags: ignoreversion recursesubdirs createallsubdirs
; Package secrets.json (built-in default API credentials, not tracked in git).
; This file must exist in the project source directory before packaging.
Source: "LumiDesk\secrets.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon\{#MyAppIconName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon\{#MyAppIconName}"; Tasks: desktopicon

[Run]
Filename: "{sys}\schtasks.exe"; Parameters: "/Create /TN ""\{#MyAppName}"" /SC ONLOGON /TR ""\""{app}\{#MyAppExeName}\"""" /RL LIMITED /F"; Flags: runhidden; Tasks: startupicon
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""\{#MyAppName}"" /F"; Flags: runhidden
