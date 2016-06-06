<#
    .DESCRIPTION
    Gets the environment variables needed to run the Killrvideo docker-compose commands.
#>
[CmdletBinding()]
Param ()

# Custom type representing the type of docker installation
Add-Type -TypeDefinition "public enum DockerInstallType { Windows, Toolbox }"

function Get-DockerType {
    <#
    .DESCRIPTION
    Determines what type of docker installation is present (Docker for Windows or Docker Toolbox)
    
    .OUTPUTS
    A DockerInstallType enum value indicating the docker environment
    #>
    [CmdletBinding()]
    Param()
    
    Write-Host 'Determining docker installation type'
    
    # Docker toolbox sets an install path environment variable so check for it
    $dt = [DockerInstallType]::Windows
    if ($Env:DOCKER_TOOLBOX_INSTALL_PATH) {
        $dt = [DockerInstallType]::Toolbox
    }
    
    Write-Verbose " => Using docker type $dt"
    $dt
}

function Get-DockerVirtualMachineIp {
    <#
    .DESCRIPTION
    Find the IP address of the docker virtual machine
    
    .PARAMETER DockerType
    The type of docker installation.
    
    .OUTPUTS
    The IP Address of the docker virtual machine
    #>
    [CmdletBinding()]
    Param (
        [parameter(Mandatory=$true)]
        [DockerInstallType]
        $DockerType
    )
    
    Write-Host 'Finding docker Virtual Machine IP'
    
    if ($DockerType -eq [DockerInstallType]::Windows) {
        # In the DfW beta, the VM will be reachable via a host named 'docker'
        $DOCKER_WINDOWS_HOST_NAME = 'docker'
        $dnsResults = Resolve-DnsName $DOCKER_WINDOWS_HOST_NAME -ErrorAction SilentlyContinue
        if ($dnsResults) {
            Write-Verbose " => Able to resolve hostname '$DOCKER_WINDOWS_HOST_NAME' at $($dnsResults.IPAddress)"
            $dnsResults.IPAddress
            return
        }
        throw "Unable to resolve host '$DOCKER_WINDOWS_HOST_NAME'. Is Docker for Windows started?"
    } else {
        # Make sure the docker VM is started
        Write-Host 'Ensuring Docker virtual machine is started'
        Invoke-Expression "docker-machine start 2>&1" | Write-Host
        
        # When using Docker Toolbox, we should be able to get the IP from docker-machine
        $dockerHost = Invoke-Expression 'docker-machine ip 2>&1'
        if ($LastExitCode -eq 0) {
            Write-Verbose " => Docker machine returned $dockerHost"
            $dockerHost
            return
        }
        
        Write-Host 'Could not resolve the Docker IP with docker-machine ip command'
        throw $dockerHost
    }
}

function Get-NetworkAddress {
    <#
    .DESCRIPTION
    Get a network address from an IP address and subnet mask
    
    .PARAMETER Address
    The IP address
    
    .PARAMETER SubnetMask
    The subnet mask
    
    .OUTPUTS
    The IP address of the network.
    #>
    [CmdletBinding()]
    Param (
        [parameter(Mandatory=$true)]
        [System.Net.IPAddress]
        $Address,
        
        [parameter(Mandatory=$false)]
        [System.Net.IPAddress]
        $SubnetMask
    )
    
    # Set default subnet mask if not provided
    if ($SubnetMask -eq $null) {
        $SubnetMask = [System.Net.IPAddress]::Parse("255.255.255.0")
    }
    
    # Get as bytes
    $ipBytes = $Address.GetAddressBytes()
    $subnetBytes = $SubnetMask.GetAddressBytes()
    
    # Create array for network bytes and use bitwise and to apply subnet mask
    $networkBytes = @()
    for ($i = 0; $i -le 3; $i++) {
        $networkBytes += $ipBytes[$i] -band $subnetBytes[$i]
    }
    
    # Return as IPAddress (constructor takes a single array as argument)
    New-Object System.Net.IPAddress -ArgumentList @(,$networkBytes)
}

function Get-HostIp {
    <#
    .DESCRIPTION
    Get the Host's IP address that's on the same network as the provided vmIp
    
    .PARAMETER VirtualMachineIPAddress
    The virtual machine's IP address
    
    .OUTPUTS
    The IP address on the host that's on the same network.
    #>
    [CmdletBinding()]
    Param (
        [parameter(Mandatory=$true)]
        [string]
        $VirtualMachineIPAddress
    )
    
    Write-Host 'Finding host IP on same network as docker Virtual Machine'
    
    $addresses = Get-NetIPAddress | ? AddressFamily -eq IPv4 | Select IPAddress, SubnetMask
    foreach($address in $addresses) {
        $vmNetworkAddress = Get-NetworkAddress -Address $VirtualMachineIPAddress -SubnetMask $address.SubnetMask
        $networkAddress = Get-NetworkAddress -Address $address.IPAddress -SubnetMask $address.SubnetMask
        
        if ($networkAddress.Equals($vmNetworkAddress)) {
            Write-Verbose " => Found host IP $($address.IPAddress)"
            $address.IPAddress
            return
        }
    }
    
    throw "Could not find a host IP address on same network as $VirtualMachineIPAddress"
}

# Figure out the network setup
$dockerType = Get-DockerType
$dockerVmIp = Get-DockerVirtualMachineIp -DockerType $dockerType
$hostIp = Get-HostIp $dockerVmIp
$isToolbox = $dockerType -eq [DockerInstallType]::Toolbox

# Write environment variable key value pairs to output
Write-Output "KILLRVIDEO_DOCKER_TOOLBOX=$($isToolbox.ToString().ToLower())"
Write-Output "KILLRVIDEO_HOST_IP=$hostIp"
Write-Output "KILLRVIDEO_DOCKER_IP=$dockerVmIp"