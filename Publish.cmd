@echo off
cd /d "%~dp0"
dotnet publish ScreenAnote.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 (pause & exit /b 1)
echo Portable executable: publish\ScreenAnote.exe
pause
