<#
    .DESCRIPTION
    Gets the environment variables needed to run the Killrvideo docker-compose commands and outputs
    them to stdout.
#>
[CmdletBinding()]
Param ()

# Figure out if we're Docker for Windows or Docker Toolbox setup
Write-Host 'Determining docker installation type'
    
# Docker toolbox sets an install path environment variable so check for it
$isToolbox = $false
if ($Env:DOCKER_TOOLBOX_INSTALL_PATH) {
    $isToolbox = $true
}

Write-Verbose " => Is Docker Toolbox: $isToolbox"

# Do things differently for Toolbox vs Docker for Windows
if ($isToolbox) {
    # See if the docker VM is running
    & docker-machine status default | Tee-Object -Variable dockerMachineStatus | Out-Null
    if ($dockerMachineStatus -ne 'Running') {
        & docker-machine start default | Out-Null
    }

    # Add environment to this shell
    & docker-machine env | Invoke-Expression
}

# Determine the Docker VM's IP address
Write-Host 'Getting Docker VM IP'
if ($isToolbox) {
    # Just use the command that comes with docker-machine
    & docker-machine ip | Tee-Object -Variable dockerIp | Out-Null
} else {
    # The VM's IP should be the IP address for eth0 when running a container in host networking mode
    $dockerIpCmd = "ip -4 addr show scope global dev eth0 | grep inet | awk `'{print `$2}`' | cut -d / -f 1"
    & docker run --rm --net=host busybox bin/sh -c $dockerIpCmd | Tee-Object -Variable dockerIp | Out-Null
}
Write-Verbose " => Got Docker IP: $dockerIp"

# Determine the VM host's IP address
Write-Host 'Getting corresponding local machine IP'
if ($isToolbox) {
    # The host only CIDR address will contain the host's IP (along with a suffix like /24)
    & docker-machine inspect --format '{{ .Driver.HostOnlyCIDR }}' default |
        Tee-Object -Variable hostCidr |
        Out-Null
    $hostIp = $hostCidr -replace "\/\d{2}", ""
} else {
    # The host's IP should be the default route for eth0 when running a container in host networking mode
    $hostIpCmd = "ip -4 route list dev eth0 0/0 | cut -d `' `' -f 3"
    & docker run --rm --net=host busybox bin/sh -c $hostIpCmd | Tee-Object -Variable hostIp | Out-Null
}
Write-Verbose " => Got Host IP: $hostIp"

# Write environment variable pairs to stdout (so this can be piped to a file)
Write-Output "KILLRVIDEO_DOCKER_TOOLBOX=$($isToolbox.ToString().ToLower())"
Write-Output "KILLRVIDEO_HOST_IP=$hostIp"
Write-Output "KILLRVIDEO_DOCKER_IP=$dockerIp"

