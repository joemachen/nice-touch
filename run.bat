@echo off
REM Builds the game and opens it. Building first means Godot never runs a
REM stale copy of the code.
setlocal
cd /d "%~dp0"

call tools\find-godot.bat
if errorlevel 1 goto :fail

echo Building...
dotnet build NiceTouch.sln --nologo -v q
if errorlevel 1 goto :fail

"%GODOT%" --path game
exit /b %errorlevel%

:fail
echo.
echo ** The game was not started. See the message above.
pause
exit /b 1
