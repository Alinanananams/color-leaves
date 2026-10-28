@echo off
REM Сборка и деплой Color Leaves (Revit 2021)
REM Требуется .NET SDK 8 или выше

setlocal

echo Building Debug ...
dotnet build build\ColorLeaves.sln -c Debug
if errorlevel 1 goto :error

echo.
echo Build complete. Деплой в %%APPDATA%%\Autodesk\Revit\Addins\2021 выполнен при сборке.
pause
exit /b 0

:error
echo.
echo ОШИБКА СБОРКИ. Подробности выше.
pause
exit /b 1
