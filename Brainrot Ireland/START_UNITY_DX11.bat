@echo off
setlocal
rem Close Unity Editor before launching this file.
set "BRAINROT_UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"
if not exist "%BRAINROT_UNITY_EDITOR%" (
  echo Unity 6000.3.11f1 was not found at the default Unity Hub location.
  echo Install this version in Unity Hub or edit BRAINROT_UNITY_EDITOR in this file.
  pause
  exit /b 1
)
if not exist "%~dp0ProjectSettings\ProjectVersion.txt" (
  echo Keep this file inside the Brainrot Ireland project folder.
  pause
  exit /b 1
)
echo Starting Brainrot Ireland with DirectX 11.
echo Recommended project location: C:\UnityProjects\BrainrotIreland
start "" "%BRAINROT_UNITY_EDITOR%" -projectPath "%~dp0." -force-d3d11
endlocal
