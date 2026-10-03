@echo off
cd /d "%~dp0"
dotnet run --project ScreenAnote.csproj -c Release
if errorlevel 1 pause
