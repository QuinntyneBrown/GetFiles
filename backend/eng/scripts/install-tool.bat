@echo off
setlocal

set TOOL_NAME=QuinntyneBrown.GetFiles
set PROJECT_PATH=%~dp0..\..\src\GetFiles\GetFiles.csproj

echo === Building GetFiles ===
dotnet pack "%PROJECT_PATH%" -c Release -o "%~dp0..\..\artifacts"
if %errorlevel% neq 0 (
    echo ERROR: Build failed.
    exit /b 1
)

echo.
echo === Uninstalling existing tool (if installed) ===
dotnet tool uninstall -g %TOOL_NAME% 2>nul
if %errorlevel% equ 0 (
    echo Tool uninstalled.
) else (
    echo Tool was not installed. Skipping uninstall.
)

echo.
echo === Installing latest version ===
dotnet tool install -g %TOOL_NAME% --add-source "%~dp0..\..\artifacts"
if %errorlevel% neq 0 (
    echo ERROR: Install failed.
    exit /b 1
)

echo.
echo === Done ===
gf --version
endlocal
