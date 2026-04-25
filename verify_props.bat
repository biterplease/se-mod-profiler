@echo off
setlocal

REM Pre-build validation: confirms that $(Bin64) resolved from Directory.Build.props exists.
REM Usage: verify_props.bat <Bin64Path>

set "BIN64=%~1"

if "%BIN64%"=="" (
    echo ERROR: Bin64 path argument is empty. Check Directory.Build.props. 1>&2
    exit /b 1
)

if not exist "%BIN64%\" (
    echo ERROR: Bin64 path does not exist: "%BIN64%". Ensure Space Engineers is installed and Directory.Build.props is correct. 1>&2
    exit /b 1
)

exit /b 0
