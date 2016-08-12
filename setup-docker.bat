@echo off

REM Make sure we have a .env file
powershell -NonInteractive -ExecutionPolicy Unrestricted -File .\lib\killrvideo-docker-common\create-environment.ps1 -Force

REM Capture the current code page, then switch to UTF-8
for /f "tokens=2 delims=:." %%x in ('chcp') do set cp=%%x
chcp 65001 > NUL

REM Tell docker-compose to use our file, plus the one in killrvideo-docker-common
echo COMPOSE_FILE=.\lib\killrvideo-docker-common\docker-compose.yaml;.\docker-compose.yaml >> .env

REM Change back to original code page
chcp %cp% > NUL

echo.
echo Pulling all docker dependencies

REM Pull dependencies
docker-compose pull

echo.
echo You can now start docker dependencies with 'docker-compose up -d'
