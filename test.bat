@echo off
REM Runs everything that must be green: the build, the sim tests and the
REM boot smoke test. CI runs this same file, so PASS means the same thing
REM on Joe's machine and on GitHub.
setlocal
cd /d "%~dp0"
set RESULT=FAIL

call tools\find-godot.bat
if errorlevel 1 goto :done

echo === Build ===
dotnet build NiceTouch.sln --nologo -v q
if errorlevel 1 goto :done

echo.
echo === Sim tests ===
dotnet test NiceTouch.sln --nologo --no-build -v q
if errorlevel 1 goto :done

echo.
echo === Boot smoke test ===
REM Starts the game with no window, runs 120 ticks (two seconds of game
REM time) and expects a clean exit with the sim having ticked.
set SMOKELOG=%TEMP%\nicetouch-smoke.log
"%GODOT%" --headless --path game --fixed-fps 60 --quit-after 120 > "%SMOKELOG%" 2>&1
if errorlevel 1 goto :smokefail
findstr /r /c:"sim ticks at exit: [1-9]" "%SMOKELOG%"
if errorlevel 1 goto :smokefail
set RESULT=PASS
goto :done

:smokefail
echo ** The game did not boot and tick cleanly. Its output was:
type "%SMOKELOG%"

:done
echo.
echo ==== %RESULT% ====
if not defined CI pause
if "%RESULT%"=="PASS" exit /b 0
exit /b 1
