#!/bin/sh

docker_machine () {
   # Doing this will set the proper env variables
   # From that we will get DOCKER_MACHINE_NAME
   echo "Setting docker-machine runtime env..."
   eval $(docker-machine env default)

   # Now we can use the env info to figure out the network
   echo "Determining docker-machine runtime..."

   # Find the host network info
   hostonly_interface=$(VBoxManage showvminfo $DOCKER_MACHINE_NAME| grep "Host-only Interface"|awk '{print $8}'|sed s/[\'\,]//g  2>&1)
   echo "Host-only interface: " $hostonly_interface

   # Use the interface to find the IP address of the host
   # Check if we are running a Mac
   if hash ipconfig 2>/dev/null; then
      host_ip=$(ifconfig $hostonly_interface | grep "inet "|awk '{print $2}')
   else
      host_ip=$(ifconfig $hostonly_interface | grep 'inet addr'|awk '{print $2}')
   fi
   
   DOCKER_IP=$(docker-machine ip $DOCKER_MACHINE_NAME 2>&1)
}

if hash docker-machine 2>/dev/null; then
        docker_machine
else
        echo "Docker-machine not running"
fi


echo "Docker IP: " $DOCKER_IP
echo "Host IP: " $host_ip

export KILLRVIDEO_DOCKER_TOOLBOX=true
export KILLRVIDEO_HOST_IP=$host_ip
export KILLRVIDEO_DOCKER_IP=$DOCKER_IP
