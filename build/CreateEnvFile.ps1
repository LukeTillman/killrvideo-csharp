<#
  .DESCRIPTION
  Create a docker compose .env file at the path specified.

  .PARAMETER FilePath
  The path to the file to create.

  .PARAMETER Force
  Tells the script to create the file even if it already exists
#>
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string]
    $FilePath
)

$scriptPath = Split-Path -parent $PSCommandPath
$getEnvCommand = Resolve-Path "$scriptPath\..\lib\killrvideo-docker-common\get-environment.ps1"

# Base environment variables to be written
$dockerEnv = @("COMPOSE_PROJECT_NAME=killrvideocsharp")

# Get the docker environment using the common script and add to array
& "$getEnvCommand" |% { $dockerEnv += $_ }

# We have to use .NET to do this so it gets written as UTF-8 without the BOM
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($FilePath, $dockerEnv, $Utf8NoBom)
Write-Host "Environment file written to $FilePath"