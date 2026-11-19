@echo off
setlocal
pushd "%~dp0.."
where py >nul 2>nul
if errorlevel 1 (
    python Tools\run_alpha_checks.py %*
) else (
    py -3 Tools\run_alpha_checks.py %*
)
set "BRAINROT_CHECK_EXIT=%errorlevel%"
popd
echo.
echo Check Logs\AlphaChecks\summary.json for the actual result.
echo Missing Unity or Python does not mean that the game passed.
pause
exit /b %BRAINROT_CHECK_EXIT%
