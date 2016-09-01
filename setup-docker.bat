@echo off

REM Make sure we have a .env file
powershell -NonInteractive -ExecutionPolicy Unrestricted -File .\lib\killrvideo-docker-common\create-environment.ps1 -Force

if %errorlevel% neq 0 exit /b %errorlevel%

REM Figure out if we're in a Docker Toolbox environment
for /F "delims== tokens=1,2" %%i in (.env) do (
  if %%i equ KILLRVIDEO_DOCKER_TOOLBOX set is_toolbox=%%j
)

REM If so, load docker-machine environment variables so pull will work
if %is_toolbox% equ true (
  @FOR /f "tokens=*" %%i IN ('docker-machine env') DO @%%i
)

echo.
echo Pulling all docker dependencies

REM Pull dependencies
docker-compose pull

if %errorlevel% neq 0 exit /b %errorlevel%

echo.
echo You can now start docker dependencies with 'docker-compose up -d'