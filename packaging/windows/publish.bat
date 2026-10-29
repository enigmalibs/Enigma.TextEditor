@echo off
rem Publishes Enigma Text Editor (Enigma.TextEditor) for Windows x64.
rem
rem Runs, from wherever it is started:
rem
rem     dotnet publish src\Enigma.TextEditor.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true
rem
rem A Release build for win-x64, compiled ReadyToRun so it starts faster, which runs on an installed
rem .NET 10 runtime. The result lands in
rem
rem     src\Enigma.TextEditor.Desktop\bin\Release\net10.0\win-x64\publish\
rem
rem and dotnet's last line names that folder. Start Enigma.TextEditor.Desktop.exe in it; app.ico beside it
rem is the icon, for a shortcut.
rem
rem publish.ps1, in this same directory, runs the same command from PowerShell. This one needs no
rem PowerShell and no execution policy: run packaging\windows\publish.bat from a command prompt.

setlocal

rem Resolved from this script (%~dp0, with its trailing backslash) rather than the current directory,
rem so it runs from anywhere.
set "PROJECT=%~dp0..\..\src\Enigma.TextEditor.Desktop"

dotnet publish "%PROJECT%" -c Release -r win-x64 -p:PublishReadyToRun=true

rem Pass dotnet's exit code on, so a failed publish fails the script.
exit /b %ERRORLEVEL%
