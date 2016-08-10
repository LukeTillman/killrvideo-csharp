#!/bin/sh

set -e  # Bail if something fails

# TODO: Determine if a Docker Toolbox setup
IS_TOOLBOX=false

if [ "$IS_TOOLBOX" = true ]; then
    echo "TODO: Docker machine environment setup"
fi

# Get the docker VM's IP address
DOCKER_IP_CMD="ip -4 addr show scope global dev eth0 | grep inet | awk '{print \$2}' | cut -d / -f 1"
DOCKER_IP=$(docker run --rm --net=host busybox bin/sh -c "$DOCKER_IP_CMD")

# Get the docker VM Host's IP address
HOST_IP_CMD="ip -4 route list dev eth0 0/0 | cut -d ' ' -f 3"
HOST_IP=$(docker run --rm --net=host busybox bin/sh -c "$HOST_IP_CMD")

# Write values to stdout
echo "KILLRVIDEO_DOCKER_TOOLBOX=$IS_TOOLBOX"
echo "KILLRVIDEO_HOST_IP=$HOST_IP"
echo "KILLRVIDEO_DOCKER_IP=$DOCKER_IP"