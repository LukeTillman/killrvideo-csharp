#!/bin/bash

set -e  # Bail if something fails

# Create the .env file
echo 'Creating docker .env file (this may take a minute)'
echo
./lib/killrvideo-docker-common/create-environment.sh

echo
echo 'Pulling all docker dependencies' 
echo

# Pull all docker dependencies
docker-compose pull

echo
echo "You can now start docker dependencies with 'docker-compose up -d'"
