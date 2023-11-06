#define AppName "Pisces"
#define DotNetVersion = "net48"
#define SrcApp "bin\debug\"+DotNetVersion+"\Pisces.exe"
#define FileVerStr GetVersionNumbersString(SrcApp)
#define public StripBuild(str aVerStr) Copy(aVerStr, 1, RPos(".", aVerStr)-1)
#define AppVerStr StripBuild(FileVerStr)

[Setup]
AppId={{9DC6B8F6-D59D-491E-8FCC-D601FE200836}
AppName={#AppName}
AppVersion={#AppVerStr}
AppVerName={#AppName} {#AppVerStr}
VersionInfoVersion={#AppVerStr}
VersionInfoTextVersion={#AppVerStr}
OutputBaseFilename=PiscesSetup_v{#AppVerStr}
;show dialogs even if previous install found
DisableDirPage=no
DisableProgramGroupPage=no
AlwaysShowDirOnReadyPage=yes
AlwaysShowGroupOnReadyPage=yes


LicenseFile=..\..\license.md

DefaultDirName={sd}\Pisces3
DefaultGroupName=Pisces

Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest

SetupIconFile=".\images\Fish_icon_3.ico"
UninstallDisplayName={#AppName} {#AppVerStr}
UninstallDisplayIcon={app}\images\Fish_icon_3.ico

;clear previously installed files
[InstallDelete]
Type: filesandordirs; Name: "{app}"


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
 

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked


[Files]
; dll's
Source:  "bin\debug\{#DotNetVersion}\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; Pisces
Source:  "bin\debug\{#DotNetVersion}\Pisces.exe";   DestDir: "{app}";   Flags: ignoreversion
Source:  "bin\debug\{#DotNetVersion}\plugins.txt";   DestDir: "{app}";   Flags: ignoreversion
Source:  "bin\debug\{#DotNetVersion}\Pisces.exe.config";   DestDir: "{app}";   Flags: ignoreversion
Source:  "..\..\private.config";   DestDir: "{app}";   Flags: ignoreversion
Source:  "bin\debug\{#DotNetVersion}\images\*";   DestDir: "{app}\images";   Flags: ignoreversion

; Hec-dss
Source:  "bin\debug\{#DotNetVersion}\bin64\Hec.Dss.Native.dll";   DestDir: "{app}\bin64";   Flags: ignoreversion
Source:  "bin\debug\{#DotNetVersion}\bin32\Hec.Dss.Native.dll";   DestDir: "{app}\bin32";   Flags: ignoreversion

; SQLite
Source:  "bin\debug\{#DotNetVersion}\x86\SQLite.Interop.dll";   DestDir: "{app}\x86";   Flags: ignoreversion
Source:  "bin\debug\{#DotNetVersion}\x64\SQLite.Interop.dll";   DestDir: "{app}\x64";   Flags: ignoreversion

; config folder
Source:  "bin\debug\{#DotNetVersion}\cfg\*";   DestDir: "{app}\cfg";   Flags: ignoreversion recursesubdirs

; Sample Data and templates
Source:  "..\..\PiscesTestData\data\el68d_export.csv";   DestDir: "{app}\sample-data";   Flags: ignoreversion
Source:  "..\..\PiscesTestData\data\wateryear.xls";   DestDir: "{app}\sample-data";   Flags: ignoreversion
Source:  "..\..\PiscesTestData\data\bulk-import-template.xlsx";   DestDir: "{app}\sample-data";   Flags: ignoreversion
Source:  "..\..\PiscesTestData\data\ac_flow.xls";   DestDir: "{app}\sample-data";   Flags: ignoreversion                                              


; NOTE: Don't use "Flags: ignoreversion" on any shared system files
[Icons]
Name: "{group}\Pisces3"; Filename: "{app}\{#AppName}.exe"; IconFilename: "{app}\images\Fish_icon_3.ico"
Name: "{autodesktop}\Pisces3"; Filename: "{app}\{#AppName}.exe"; Tasks: desktopicon; IconFilename: "{app}\images\Fish_icon_3.ico"


[Code]
// from http://www.kynosarges.de/DotNetVersion.html
function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1'          .NET Framework 1.1
//    'v2.0'          .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//    'v4.5.1'        .NET Framework 4.5.1
//    'v4.5.2'        .NET Framework 4.5.2
//    'v4.6'          .NET Framework 4.6
//    'v4.6.1'        .NET Framework 4.6.1
//    'v4.6.2'        .NET Framework 4.6.2
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key, versionKey: string;
    install, release, serviceCount, versionRelease: cardinal;
    success: boolean;
begin
    versionKey := version;
    versionRelease := 0;

    // .NET 1.1 and 2.0 embed release number in version key
    if version = 'v1.1' then begin
        versionKey := 'v1.1.4322';
    end else if version = 'v2.0' then begin
        versionKey := 'v2.0.50727';
    end

    // .NET 4.5 and newer install as update to .NET 4.0 Full
    else if Pos('v4.', version) = 1 then begin
        versionKey := 'v4\Full';
        case version of
          'v4.5':   versionRelease := 378389;
          'v4.5.1': versionRelease := 378675; // 378758 on Windows 8 and older
          'v4.5.2': versionRelease := 379893;
          'v4.6':   versionRelease := 393295; // 393297 on Windows 8.1 and older
          'v4.6.1': versionRelease := 394254; // 394271 on Windows 8.1 and older
          'v4.6.2': versionRelease := 394802; // 394806 on Windows 8.1 and older
        end;
    end;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0 and newer use value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 and newer use additional value Release
    if versionRelease > 0 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= versionRelease);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;


function InitializeSetup(): Boolean;
begin
    if not IsDotNetDetected('v4.5.2', 0) then begin
        MsgBox('MyApp requires Microsoft .NET Framework 4.5.2'#13#13
            'Please use Windows Update to install this version,'#13
            'and then re-run the MyApp setup program.', mbInformation, MB_OK);
        result := false;
    end else
        result := true;
end;
