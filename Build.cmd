@echo off
cd /d "%~dp0"
dotnet build ScreenAnote.csproj -c Release
if errorlevel 1 (pause & exit /b 1)
echo Build complete. Use Run.cmd to launch.
pause
