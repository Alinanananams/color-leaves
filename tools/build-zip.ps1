# Сборка ColorLeaves-R21.zip для ручной установки.
# Структура: ColorLeaves.addin в корне архива, файлы плагина — в папке ColorLeaves\.
# Архив распаковывается целиком в %APPDATA%\Autodesk\Revit\Addins\2021\.
# Требуется сначала собрать Release: dotnet build build\ColorLeaves.sln -c Release
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$version = (Get-Content (Join-Path $root "Version.txt") -Raw).Trim()
$bin = Join-Path $root "src\ColorLeaves.Revit\bin\Release\net48"
$res = Join-Path $root "src\ColorLeaves.Revit\Resources"
$addin = Join-Path $root "addins\2021\ColorLeaves.addin"

if (-not (Test-Path (Join-Path $bin "ColorLeaves.Revit.dll"))) {
    throw "Release-сборка не найдена. Сначала выполните: dotnet build build\ColorLeaves.sln -c Release"
}

# Стейджинг: .addin в корне, остальное в ColorLeaves\
$stage = Join-Path $env:TEMP ("ColorLeaves-zip-" + [guid]::NewGuid().ToString("N").Substring(0, 8))
New-Item -ItemType Directory -Path (Join-Path $stage "ColorLeaves") | Out-Null

Copy-Item $addin (Join-Path $stage "ColorLeaves.addin")
Copy-Item (Join-Path $bin "ColorLeaves.Revit.dll") (Join-Path $stage "ColorLeaves")
Copy-Item (Join-Path $bin "ColorLeaves.Core.dll") (Join-Path $stage "ColorLeaves")
foreach ($png in @("sheet.png", "parameter.png", "settings.png")) {
    Copy-Item (Join-Path $res $png) (Join-Path $stage "ColorLeaves")
}

$zip = Join-Path $root "ColorLeaves-R21.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -Force
Remove-Item $stage -Recurse -Force

Write-Host ("Zip built: {0} (версия {1})" -f $zip, $version)
