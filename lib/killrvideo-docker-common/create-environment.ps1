<#
    .DESCRIPTION
    Gets the environment variables needed to run the Killrvideo docker-compose commands, then writes
    them to a .env file in the working directory or the directory specified by the -Path switch.

    .PARAMETER Path
    The path to create the environment file. Defaults to the current working directory.

    .PARAMETER FileName
    The name of the environment file. Defaults to ".env".

    .PARAMETER ProjectName
    The COMPOSE_PROJECT_NAME value to use. Defaults to "killrvideo".

    .PARAMETER Force
    Switch to force creation of the file (i.e. overwrite it) if it already exists.
#>
[CmdletBinding()]
Param (
    [Parameter(Mandatory=$false)]
    [string]
    $Path = $((Resolve-Path .\).Path),

    [Parameter(Mandatory=$false)]
    [string]
    $FileName = '.env',

    [Parameter(Mandatory=$false)]
    [string]
    $ProjectName = 'killrvideo',

    [Parameter(Mandatory=$false)]
    [switch]
    $Force
)

$ErrorActionPreference = 'stop'

# Make sure we have an absolute path
if ([System.IO.Path]::IsPathRooted($Path) -eq $false) {
    $cwd = (Resolve-Path .\).Path
    $Path = Join-Path $cwd $Path
}

# Path to the file and does it exist
$envFilePath = Join-Path $Path $FileName
if ((Test-Path $envFilePath) -and ($Force -eq $false)) {
    Write-Host "Environment file $(Resolve-Path $envFilePath) already exists, will not overwrite"
    Exit
}

# Make sure the path exists for the .env we're generating
if ((Test-Path $Path) -eq $false) {
    New-Item -Path $Path -Type Directory | Out-Null
}

$scriptPath = Split-Path -parent $PSCommandPath

# Use the base compose file from this project, plus one that should be in the same location
# as the .env file we're generating
Push-Location $Path
$composeFile = Resolve-Path -Relative "$scriptPath\docker-compose.yaml"
Pop-Location
$composeFile += ";.\docker-compose.yaml"

# Base environment variables
$dockerEnv = @("COMPOSE_PROJECT_NAME=$ProjectName", "COMPOSE_FILE=$composeFile")

# Get path to the get-environment script and run it, adding each value to the env array
$getEnvCommand = Resolve-Path "$scriptPath\get-environment.ps1"
& "$getEnvCommand" |% { $dockerEnv += $_ }

# Write the file (have to use the .NET API here because we need UTF-8 WITHOUT the BOM)
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($envFilePath, $dockerEnv, $Utf8NoBom)
Write-Host "Environment file written to $(Resolve-Path $envFilePath)"