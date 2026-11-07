; Inno Setup 6 script. Build: iscc installer\STX.iss /DAppVersion=2.1.0  (after scripts\package-release.ps1)
; NOT TESTED in the audit environment - build and test install / upgrade / uninstall on a clean Windows VM.
#ifndef AppVersion
  #define AppVersion "2.1.0"
#endif
[Setup]
AppId={{FED2B510-04BB-406A-AAE6-D333B081EC47}
AppName=STX Enterprises
AppVersion={#AppVersion}
AppPublisher=STX Networks
DefaultDirName={autopf}\STX Enterprises
DefaultGroupName=STX Enterprises
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
OutputDir=..\dist
OutputBaseFilename=STX-Enterprises-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
UninstallDisplayIcon={app}\STXEnterprises.exe
CloseApplications=yes
[Files]
Source: "..\dist\stage\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; Excludes: "guiConfigs\*,guiLogs\*,binConfigs\*"
[Icons]
Name: "{group}\STX Enterprises"; Filename: "{app}\STXEnterprises.exe"
Name: "{autodesktop}\STX Enterprises"; Filename: "{app}\STXEnterprises.exe"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"
[Run]
Filename: "{app}\STXEnterprises.exe"; Description: "Start STX Enterprises"; Flags: nowait postinstall skipifsilent
[UninstallRun]
; make sure Windows is not left pointing at a dead local proxy, then stop any leftover cores
Filename: "{app}\STXEnterprises.exe"; Parameters: "--reset-proxy"; Flags: runhidden waituntilterminated; RunOnceId: "ResetProxy"
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM STXEnterprises.exe"; Flags: runhidden; RunOnceId: "KillApp"
[UninstallDelete]
Type: filesandordirs; Name: "{app}\guiLogs"
Type: filesandordirs; Name: "{app}\guiTemps"
Type: filesandordirs; Name: "{app}\binConfigs"
Type: filesandordirs; Name: "{localappdata}\STX Enterprises"
