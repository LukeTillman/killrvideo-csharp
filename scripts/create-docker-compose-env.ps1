<#
  .DESCRIPTION
  Create a docker compose .env file
#>
$scriptPath = Split-Path -parent $PSCommandPath
$envFilePath = "$scriptPath\..\.env"
$getEnvCommand = Resolve-Path "$scriptPath\..\lib\killrvideo-docker-common\get-environment.ps1"

# Base environment variables to be written
$dockerEnv = @("COMPOSE_PROJECT_NAME=killrvideocsharp", "COMPOSE_FILE=.\lib\killrvideo-docker-common\docker-compose.yaml")

# Get the docker environment using the common script and add to array
& "$getEnvCommand" |% { $dockerEnv += $_ }

# We have to use .NET to do this so it gets written as UTF-8 without the BOM
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($envFilePath, $dockerEnv, $Utf8NoBom)
$envFilePath = Resolve-Path $envFilePath
Write-Host "Environment file written to $envFilePath" 