# KillrVideo
A sample web application showing how to use Cassandra with .NET via [DataStax .NET Driver][1].  The application is based on 
a [sample schema][2] (that has since been modified) for a video sharing site that Patrick McFadin has been using to demonstrate 
data modeling techniques with Apache Cassandra.

## Running Locally
These instructions assume you've already got a running Apache Cassandra cluster (for local development, that cluster could be 
a single instance).  If not, go grab a copy and install it from [Planet Cassandra][3].  Once that's done and you've cloned the 
repository, you'll want to do the following things to get it up and running:

1. Run the `schema.cql` script in the `data/` folder against your Cassandra cluster to create the appropriate schema.  For 
example (via CQL Shell):

    `cqlsh -f schema.cql`
    
2. Open `KillrVideo.sln` in Visual Studio or your IDE of choice.
3. If the Cassandra cluster you want to run against is *not* running on `127.0.0.1`:
  * Modify the `<appSettings>` section in the `App.config` for the *KillrVideo.SampleDataLoader* project to point at your Cassandra cluster.
  * Modify the `<appSettings>` section in the `Web.config` for the *KillrVideo* project to point at your Cassandra cluster.
4. Run the *KillrVideo.SampleDataLoader* console application to load sample data into your schema.
5. Run the *KillrVideo* web application.  It will use IIS Express by default.

## Pull Requests, Requests for More Examples
This project will continue to evolve along with Cassandra and you can expect that as Cassandra and the DataStax driver add new features, 
this sample application will try and provide examples of those.  I'll gladly accept any pull requests for bug fixes, new features, etc. 
and if you have a request for an example that you don't see in the code currently, send me a message [@LukeTillman][4] on Twitter or 
open an issue here on GitHub.

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
  [2]: https://github.com/pmcfadin/cassandra-videodb-sample-schema
  [3]: http://planetcassandra.org/cassandra/
  [4]: https://twitter.com/LukeTillman