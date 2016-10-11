#!/bin/bash

set -e  # Bail if something fails

# This script tries to create a .env file in the current working directory for 
# use with docker-compose. The file contains variables that include info on the 
# user's docker setup like the IP address of the Host and VM.

ENV_FILE_PATH="$PWD/.env"

# TODO: Don't overwrite file if it already exists?

# Relative path that contains this script
SCRIPT_PATH=${BASH_SOURCE%/*}

# Create an alias for the loopback adapter so that the Mac and Docker VM can communicate using that IP
export LOOPBACK_IP='10.0.75.1'
echo 'We need to create an alias for the loopback adapter (lo0) using sudo'
echo 'so your Mac and the Docker VM can communicate. It will be created using'
echo "IP $LOOPBACK_IP. You will be prompted for your password."
sudo ifconfig lo0 alias $LOOPBACK_IP

# Should use compose file relative to this script, followed by a compose file relative to the
# working directory (i.e. where the .env file is going to be created)
COMPOSE_FILE="$SCRIPT_PATH/docker-compose.yaml:./docker-compose.yaml"
COMPOSE_PROJECT_NAME='killrvideo'

# Get other variables from the get-environment.sh script
GET_ENV_OUTPUT=$(exec $SCRIPT_PATH/get-environment.sh)

# Write to .env file in current working directory
echo "COMPOSE_PROJECT_NAME=$COMPOSE_PROJECT_NAME
COMPOSE_FILE=$COMPOSE_FILE
$GET_ENV_OUTPUT" > $ENV_FILE_PATH