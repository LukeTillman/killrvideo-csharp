# KillrVideo
A sample video sharing application showing how to use [Apache Cassandra][11] with .NET via [DataStax .NET Driver][1] in the [Microsoft Azure][10] cloud.  The application is based on a [sample schema][2] (that has since been modified) for a video sharing site that Patrick McFadin has been using to demonstrate data modeling techniques with Apache Cassandra.

#### See the live demo
Go to [www.killrvideo.com](http://www.killrvideo.com) to see a live demo of this application hosted in Azure.

## Running Locally
Once you've cloned the repository, you'll need to do a few things to get up and running locally.

#### Prerequisites
* A running Apache Cassandra cluster (for local development, that cluster could just be a single instance).  You can get a copy from [Planet Cassandra][3] (grab the DataStax Community edition).
* The [Microsoft Azure SDK][5] so you can run the web/worker roles via the emulator locally.
* An [Azure Media Services][6] account (you can create one from the [Azure Management Portal][7]).  You can get a [free trial of Azure][8] if you don't already have an Azure account.

#### Running
1. Run the `schema.cql` script in the `data/` folder against your Cassandra cluster to create the appropriate schema.  For example (via CQL Shell):

    `cqlsh -f schema.cql`

2. Under the `/src/KillrVideo.Azure` folder, find the `ServiceConfiguration.Local.Transform.cscfg.template` configuration transform file.  Make a copy and rename it to `ServiceConfiguration.Local.Transform.cscfg` (i.e. remove the `.template` file extension).
3. Edit the `ServiceConfiguration.Local.Transform.cscfg` transformation file you just created and fill in the values for your local environment.  You'll need to provide:
  * `CassandraClusterLocation`: the IP/DNS for your Cassandra cluster (for example, `127.0.0.1`).
  * `AzureMediaServicesAccountName`: the account name for your Azure Media Services account (this is used to process video uploads).
  * `AzureMediaServicesAccountKey`: the secret key for your Azure Media Services account
  * `AzureStorageConnectionString`: the connection string for the Storage account that's linked to your Azure Media Services account.
4. Open `KillrVideo.sln` in Visual Studio.  Right-click on the *KillrVideo.Azure* project and set it as the startup project.
5. Optional:  Run the *KillrVideo.SampleDataLoader* console application to load sample data into your schema.  If your Cassandra cluster is not located at `127.0.0.1` you'll want to change the value in this application's `App.config` file to point at your Cassandra cluster.
6. Press F5 to launch the *KillrVideo.Azure* project with a debugger attached.  It should launch the Azure Emulator and IIS Express by default.  The web site will run on `127.0.0.1` by default.

#### Enabling uploads to Azure Media Services
When uploading videos to share, the client-side JavaScript uploads directly to Azure Storage (bypassing the KillrVideo web app) so as not to put additional strain on the web server just for video uploads.  It does this by taking advantage of CORS-enabled uploads for Azure Media Services.  For this to work, you'll need to add a CORS rule to your Azure Storage account.  Unfortunately, you can't manage these rules via the Azure Management Portal UI, however someone else has built a simple UI for managing these rules.

1.  Grab the [Azure CORS Rule Manager][9] web app from GitHub.
2.  Open it in Visual Studio and run it (no configuration is needed to launch the app).
3.  Enter the credentials for the Azure Storage account linked to your Azure Media Services account.
4.  Add a CORS rule (modify these to fit your local settings):
    * **Allowed origins:** http://127.0.0.1, http://youralias.local
    * **Allowed methods:** Put
    * **Allowed headers:** content-type, accept, x-ms-\*
    * **Exposed headers:** x-ms-\*
    * **Max age (seconds):** 86400

## Pull Requests, Requests for More Examples
This project will continue to evolve along with Cassandra and you can expect that as Cassandra and the DataStax driver add new features, this sample application will try and provide examples of those.  I'll gladly accept any pull requests for bug fixes, new features, etc.  and if you have a request for an example that you don't see in the code currently, send me a message [@LukeTillman][4] on Twitter or open an issue here on GitHub.

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
  [5]: http://azure.microsoft.com/en-us/downloads/
  [6]: http://azure.microsoft.com/en-us/services/media-services/
  [7]: https://manage.windowsazure.com
  [8]: http://azure.microsoft.com/en-us/pricing/free-trial/
  [9]: https://github.com/pootzko/azure-cors-rule-manager
  [10]: http://azure.microsoft.com
  [11]: http://www.datastax.com/what-we-offer/products-services/datastax-enterprise/apache-cassandra
