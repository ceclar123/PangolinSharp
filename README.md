## 基础框架
- Avalonia-11.0.10
- dotnet-8

## 发布打包win-x64
cd Pangolin.Desktop
dotnet publish Pangolin.Desktop.csproj --framework net8.0 --runtime win-x64 --configuration Release -p:UseAppHost=true -p:PublishAot=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true --self-contained true -p:AssemblyName=Pangolin.Desktop
dotnet publish Pangolin.Desktop.csproj --framework net8.0 --runtime win-x64 --configuration Release -p:UseAppHost=true -p:PublishAot=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true --self-contained true
dotnet publish Pangolin.Desktop.csproj --framework net8.0 --runtime win-arm64 --configuration Release -p:UseAppHost=true -p:PublishAot=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true --self-contained true --no-restore -p:AssemblyName=PangolinSharp

# dotnet编译自包含运行时的可执行文件，然后可以用anno setup脚本打包exe安装包
```
; 脚本由 Inno Setup 脚本向导 生成！
; 有关创建 Inno Setup 脚本文件的详细资料请查阅帮助文档！

#define MyAppName "PangolinSharp"
#define MyAppVersion "1.0"
#define MyAppPublisher "我的公司"
#define MyAppURL "https://www.example.com/"
#define MyAppExeName "PangolinSharp.exe"

[Setup]
; 注: AppId的值为单独标识该应用程序。
; 不要为其他安装程序使用相同的AppId值。
; (若要生成新的 GUID，可在菜单中点击 "工具|生成 GUID"。)
AppId={{00DF2B29-25B5-4070-86AB-84878A9813A0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\PangolinSharp
DefaultGroupName=PangolinSharp
; 以下行取消注释，以在非管理安装模式下运行（仅为当前用户安装）。
;PrivilegesRequired=lowest
OutputDir={#GetEnv('USERPROFILE')}\Desktop
OutputBaseFilename=upsauto-settup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#GetEnv('USERPROFILE')}\Desktop\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#GetEnv('USERPROFILE')}\Desktop\publish\av_libglesv2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#GetEnv('USERPROFILE')}\Desktop\publish\libHarfBuzzSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#GetEnv('USERPROFILE')}\Desktop\publish\libSkiaSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#GetEnv('USERPROFILE')}\Desktop\publish\tray.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#GetEnv('USERPROFILE')}\Desktop\publish\tray.png"; DestDir: "{app}"; Flags: ignoreversion
; 注意: 不要在任何共享系统文件上使用“Flags: ignoreversion”

[Icons]
; 设置图标
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\tray.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\tray.ico"
;设置开机自启
Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"


[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent



```


## 发布打包osx-x64
cd Pangolin.Desktop
dotnet publish Pangolin.Desktop.csproj --framework net8.0 --runtime osx-arm64 --configuration Release -p:UseAppHost=true -p:PublishAot=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true --self-contained true -p:AssemblyName=Pangolin.Desktop
dotnet publish Pangolin.Desktop.csproj --framework net8.0 --runtime osx-x64 --configuration Release -p:UseAppHost=true -p:PublishAot=false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true --self-contained true -p:AssemblyName=Pangolin.Desktop
