# KillrVideo
A reference application for learning about using [Apache Cassandra][2] and [DataStax Enterprise][4] with .NET. The
application has sample code that uses:
- The [DataStax .NET Driver][1] for Cassandra
- Google's [Grpc][8]

## Running Locally
The application uses [Docker][5] to spin up all of its dependencies. You'll need to have it installed before trying to
start the application locally. You have two options on Windows:
- [Docker for Windows Beta][6]: Currently works on Windows 10 Professional Edition
- [Docker Toolbox][7]: Works on older versions of Windows (7, 8) and Windows 10 Home Edition

Once you have Docker installed and running, you should be able to `git clone` this repo and get it running. To run the
application:
1. Open the main solution at `/src/KillrVideo.sln` in Visual Studio.
1. Set the **KillrVideo** project as the Startup Project (right click on the KillrVideo project and choose *Set as Startup Project*).
1. Press `F5` to build and run.

Once the application has started successfully, you'll see output in console telling you the address where the web UI is 
available. You can open a web browser to that address to start using the application.

## Details on Docker Usage
The first time you build the application in Visual Studio, it will run a script as part of the build to try determine your
Docker environment and write those details to an `.env` file. Then any time the application is run, it will try and
start up its infrastructure dependencies first via `docker-compose`. It starts the following dependencies in Docker
containers:
- A one node [DataStax Enterprise][4] cluster: used for all the data storage by the application
- An [etcd][9] node: used as a registry of services for service discovery
- The [KillrVideo Web server][10]: serves up the UI and makes calls to the backend services

## Pull Requests, Requests for More Examples
This project will continue to evolve along with Cassandra and you can expect that as Cassandra and the DataStax driver add new features, this sample application will try and provide examples of those.  I'll gladly accept any pull requests for bug fixes, new features, etc.  and if you have a request for an example that you don't see in the code currently, send me a message [@LukeTillman][3] on Twitter or open an issue here on GitHub.

## License
Copyright 2014 Luke Tillman

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

  [1]: https://github.com/datastax/csharp-driver
  [2]: http://planetcassandra.org
  [3]: https://twitter.com/LukeTillman
  [4]: http://www.datastax.com/products/datastax-enterprise
  [5]: https://www.docker.com/
  [6]: https://www.docker.com/products/docker#/windows
  [7]: https://www.docker.com/products/docker-toolbox
  [8]: http://www.grpc.io/
  [9]: https://github.com/coreos/etcd
  [10]: https://github.com/KillrVideo/killrvideo-web
