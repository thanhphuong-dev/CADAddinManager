@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "PLUGIN_DIR=%~dp0"
if "%PLUGIN_DIR:~-1%"=="\" set "PLUGIN_DIR=%PLUGIN_DIR:~0,-1%"
set "DLL_PATH=%PLUGIN_DIR%\CADAddinManager.dll"
if not exist "%DLL_PATH%" ( echo [THAT BAI] Khong tim thay %DLL_PATH% & pause & exit /b 1 )
set "TARGET_VERSIONS=2021 2026"
for %%D in ("%PLUGIN_DIR%") do set "PACKAGE_NAME=%%~nxD"
if /i "%PACKAGE_NAME%"=="zwcad-2021-release" set "TARGET_VERSIONS=2021"
if /i "%PACKAGE_NAME%"=="zwcad-2026-release" set "TARGET_VERSIONS=2026"
set /a INSTALLED_COUNT=0
for %%V in (%TARGET_VERSIONS%) do for %%L in (en-US vi-VN en-WW zh-CN) do (
  set "ROOT_KEY=HKCU\SOFTWARE\ZWSOFT\ZWCAD\%%V\%%L"
  reg query "!ROOT_KEY!" >nul 2>&1
  if !errorlevel! == 0 (
    set "APP_KEY=!ROOT_KEY!\Applications\CADAddinManager"
    reg add "!APP_KEY!" /f >nul
    reg add "!APP_KEY!" /v Managed /t REG_DWORD /d 1 /f >nul
    reg add "!APP_KEY!" /v LOADCTRLS /t REG_DWORD /d 2 /f >nul
    reg add "!APP_KEY!" /v Loader /t REG_SZ /d "%DLL_PATH%" /f >nul
    reg add "!APP_KEY!" /v DESCRIPTION /t REG_SZ /d "CAD Add-in Manager" /f >nul
    echo [OK] ZWCAD %%V %%L
    set /a INSTALLED_COUNT+=1
  )
)
reg add "HKCU\SOFTWARE\CADAddinManager\ZWCAD" /v PluginPath /t REG_SZ /d "%PLUGIN_DIR%" /f >nul
if !INSTALLED_COUNT! GTR 0 ( echo Da dang ky !INSTALLED_COUNT! phien ban. Hay khoi dong lai ZWCAD. ) else ( echo Khong tim thay ZWCAD trong Registry; dung NETLOAD de nap DLL. )
pause
endlocal
