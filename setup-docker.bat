@echo off

REM Make sure we have a .env file
powershell -NonInteractive -ExecutionPolicy Unrestricted -File .\lib\killrvideo-docker-common\create-environment.ps1 -Force

echo.
echo Pulling all docker dependencies

REM Pull dependencies
docker-compose pull

echo.
echo You can now start docker dependencies with 'docker-compose up -d'
