@echo off
REM Build Color Leaves setup exe (files embedded as resources)
REM Requires: Release build first (dotnet build build\ColorLeaves.sln -c Release)

setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set BIN=%~dp0..\src\ColorLeaves.Revit\bin\Release\net48
set RESDIR=%~dp0..\src\ColorLeaves.Revit\Resources
set ADDIN=%~dp0..\addins\2021\ColorLeaves.addin
set SRC=%~dp0installer\Installer.cs

for /f "usebackq" %%v in ("%~dp0..\Version.txt") do set VER=%%v
set OUT=%~dp0..\ColorLeaves-%VER%-setup.exe

if not exist "%BIN%\ColorLeaves.Revit.dll" (
    echo ERROR: build Release first: dotnet build build\ColorLeaves.sln -c Release
    exit /b 1
)

pushd "%BIN%"
"%CSC%" /nologo /target:winexe /out:"%OUT%" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll ^
  "/resource:ColorLeaves.Revit.dll" "/resource:ColorLeaves.Core.dll" ^
  "/resource:%ADDIN%" ^
  "/resource:%RESDIR%\sheet.png" "/resource:%RESDIR%\refresh.png" "/resource:%RESDIR%\colors.png" "/resource:%RESDIR%\parameter.png" "/resource:%RESDIR%\settings.png" ^
  "/resource:%~dp0..\Version.txt" ^
  "%SRC%"
popd

if exist "%OUT%" (
    echo Setup built: %OUT%
) else (
    echo ERROR: setup build failed
    exit /b 1
)
