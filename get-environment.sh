#!/bin/sh

set -e  # Bail if something fails

# This script will try and detect a user's docker setup and write some environment
# variable pairs to stdout. The stdout output from this script can then be used to,
# for example, create a .env file for use with docker-compose 

# TODO: Determine if a Docker Toolbox setup
IS_TOOLBOX=false

if [ "$IS_TOOLBOX" = true ]; then
    # Make sure default docker machine is started
    STATUS=$(docker-machine status default)
    if [ "$STATUS" != "Running" ]; then
        docker-machine start default > /dev/null
    fi

    # Load docker machine env into this shell
    eval $(docker-machine env default)
fi

# Get the docker VM's IP address
if [ "$IS_TOOLBOX" = true ]; then
    # Just use the command that comes with docker-machine
    DOCKER_IP=$(docker-machine ip)
else
    # The create-environment.sh script should have setup a loopback alias, so use that IP
    DOCKER_IP=$LOOPBACK_IP
fi

# Get the docker VM Host's IP address
if [ "$IS_TOOLBOX" = true ]; then
    # The host only CIDR address will contain the host's IP (along with a suffix like /24)
    HOST_IP=$(docker-machine inspect --format '{{ .Driver.HostOnlyCIDR }}' default)
    # Remove suffix
    HOST_IP=${HOST_IP//\/[[:digit:]][[:digit:]]/}
else
    # The create-environment.sh script should have setup a loopback alias, so use that IP
    HOST_IP=$LOOPBACK_IP
fi

# Write values to stdout
echo "KILLRVIDEO_DOCKER_TOOLBOX=$IS_TOOLBOX"
echo "KILLRVIDEO_HOST_IP=$HOST_IP"
echo "KILLRVIDEO_DOCKER_IP=$DOCKER_IP"