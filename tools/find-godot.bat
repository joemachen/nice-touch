@echo off
REM Sets GODOT to the Godot executable, or fails with a clear message.
REM Called by run.bat and test.bat. Godot lives outside the repo, so the
REM path comes from the GODOT environment variable when that is set.

if "%GODOT%"=="" set GODOT=D:\Projects\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe
if exist "%GODOT%" exit /b 0

echo ** Godot was not found at: %GODOT%
echo ** Install Godot 4.7.1 (.NET build), or set the GODOT environment
echo ** variable to its executable, then run this again.
exit /b 1
