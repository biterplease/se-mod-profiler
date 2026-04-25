@echo off
setlocal enabledelayedexpansion

if "%~2" == "" (
    echo ERROR: Missing required parameters
    exit /b 1
)

set NAME=%~1
set SOURCE=%~2

set "SRCFILE="
if exist "%SOURCE%\%NAME%" (
    set "SRCFILE=%SOURCE%\%NAME%"
) else if exist "%SOURCE%" (
    set "SRCFILE=%SOURCE%"
) else (
    echo ERROR: Source not found: %SOURCE% or %SOURCE%\%NAME%
    exit /b 1
)

set PLUGIN_DIR=%AppData%\Pulsar\Legacy\Local
if not exist "%PLUGIN_DIR%" (
    echo Missing Local plugin folder: %PLUGIN_DIR%
    echo Pulsar not installed?
    exit /b 2
)

echo Copying "%SRCFILE%" to "%PLUGIN_DIR%\"
copy /y "%SRCFILE%" "%PLUGIN_DIR%\"
if !ERRORLEVEL! NEQ 0 (
    echo ERROR: Could not copy "%NAME%", make sure the game is not running and try again.
    exit /b 1
)

exit /b 0
