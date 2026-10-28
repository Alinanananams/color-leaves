@echo off
REM Сборка и деплой Color Leaves
REM Требуется .NET SDK 8 или выше

setlocal

REM Собираем для каждой конфигурации
for %%c in (Debug.R21, Debug.R22, Debug.R23, Debug.R24, Debug.R25) do (
    echo Building %%c ...
    dotnet build build\ColorLeaves.sln -c %%c
)

echo Build complete.
pause
