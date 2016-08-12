@echo off

REM Make sure we have a .env file
powershell -NonInteractive -ExecutionPolicy Unrestricted -File .\lib\killrvideo-docker-common\create-environment.ps1 -Force

REM Use Utf-8 when adding compose file below
chcp 65001

REM Tell docker-compose to use our file, plus the one in killrvideo-docker-common
echo COMPOSE_FILE=.\lib\killrvideo-docker-common\docker-compose.yaml;.\docker-compose.yaml >> .env

echo.
echo You can now start docker dependencies with 'docker-compose up -d'
