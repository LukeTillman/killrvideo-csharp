# KillrVideo
A sample video sharing application showing how to use [Apache Cassandra][11] with .NET via [DataStax .NET Driver][1] in the [Microsoft Azure][10] cloud.  The application is based on a [sample schema][2] (that has since been modified) for a video sharing site that Patrick McFadin has been using to demonstrate data modeling techniques with Apache Cassandra.

#### A More In-Depth Introduction
If you'd like a more in-depth introduction to the application, including a tour of the code structure, architecture, and some of the motivations, check out my blog post [KillrVideo: Cassandra, C# and Azure][17].  I also have a ongoing series of blog posts on KillrVideo which can be found under the [killrvideo tag][19] on my blog.

#### See the live demo
A live demo of KillrVideo running on Cassandra in Azure is available at [www.killrvideo.com][16].

## Running Locally
Once you've cloned the repository, you'll need to do a few things to get up and running locally.

#### Install Cassandra
You'll need a running Cassandra cluster.  For local development, that cluster can just be a single node running on your local machine.  You can get a copy from [Planet Cassandra][3] (grab the DataStax Community edition).  If you're on Windows, the MSI installer from Planet Cassandra makes installing a node on your local development machine easy.  If you'd like more details on installing and developing with Cassandra on Windows, check out my blog post aptly titled [Developing with Cassandra on Windows][18].

#### Install the Microsoft Azure SDK
The [Microsoft Azure SDK][5] will allow you to run Azure web/worker roles via an emulator on your local machine.  If you don't already have it, you'll need to install it.

#### Create Azure Services keys
KillrVideo makes use of a number of Microsoft Azure services.  As a result, you'll need to provision the appropriate resources in the [Azure Management Portal][7] under your account.  If you don't already have an Azure account, you can get a [free trial of Azure][8] from Microsoft.  From the [management portal][7], you'll need to create:
* An [Azure Storage][13] account to use for Azure Media Services.  This is used to store uploaded videos from the app.
* An [Azure Media Services][6] account.  The portal will ask you to select an Azure Storage account when you create the Media Services account (select the one you just created previously).  This is used to process and encode uploaded videos for playback.
* An [Azure Service Bus][12] namespace.  This is used for communication between services (via events) in the application.

#### Create a YouTube API key
KillrVideo allows adding videos to the site from sources other than uploads (for example, YouTube).  As a result, you'll need to create a YouTube API key.  Follow the [instructions from Google][14] for creating a project and adding a key in the Google Developers console.
> Note: You don't need an OAuth 2.0 key, just a regular API key.  You'll want to create a **Browser Key** when prompted by the console.

#### Create the KillrVideo schema in Cassandra
Run the `schema.cql` script in the `data/` folder against your Cassandra cluster to create the appropriate schema.  For example (via CQL Shell):

    cqlsh -f schema.cql

If you'd like a GUI for exploring Cassandra (and running the CQL script to create the schema), check out the free [Dev Center][15] tool for developers.

#### Configure the KillrVideo Application
You'll need to configure your local copy of KillrVideo with the correct connection strings and API keys that you created.  Here are the steps:

1. Under the `/src/KillrVideo.Azure` folder, find the `ServiceConfiguration.Local.Transform.cscfg.template` configuration transform file.  Make a copy and rename it to `ServiceConfiguration.Local.Transform.cscfg` (i.e. remove the `.template` file extension).
1. Edit the `ServiceConfiguration.Local.Transform.cscfg` transformation file you just created and fill in the values for your local environment.  You'll need to provide:
  * `CassandraClusterLocation`: the IP/DNS for your Cassandra cluster (for example, `127.0.0.1`).
  * `AzureMediaServicesAccountName`: the account name for your Azure Media Services account.
  * `AzureMediaServicesAccountKey`: the secret key for your Azure Media Services account
  * `AzureStorageConnectionString`: the connection string for the Storage account that's linked to your Azure Media Services account.
  * `AzureServiceBusConnectionString`: the connection string for the Service Bus namespace that you created.
  * `YouTubeApiKey`: the YouTube API key that you created.

#### Run KillrVideo
Open `KillrVideo.sln` in Visual Studio.  Right-click on the *KillrVideo.Azure* project and set it as the startup project.  Press F5 to launch the *KillrVideo.Azure* project with a debugger attached.  It should launch the Azure Emulator and IIS Express by default.  The web site will run on `127.0.0.1` by default.

#### Adding Sample Data to KillrVideo
KillrVideo contains a SampleData service that will periodically add sample YouTube videos, users, comments, etc. to the site on a schedule.  The first time you run the application, some of that data should be added (you will probably need to refresh the home page of the web application after about 30 seconds to see the initial sample videos).  When running locally, you can also manually trigger adding sample data to the site using the *Add Sample Data* link in the footer of the site.

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
  [12]: http://azure.microsoft.com/en-us/services/service-bus/
  [13]: http://azure.microsoft.com/en-us/services/storage/
  [14]: https://developers.google.com/youtube/registering_an_application
  [15]: http://www.datastax.com/what-we-offer/products-services/devcenter
  [16]: http://www.killrvideo.com
  [17]: http://www.luketillman.com/killrvideo-cassandra-csharp-and-azure/
  [18]: http://www.luketillman.com/developing-with-cassandra-on-windows/
  [19]: http://www.luketillman.com/tag/killrvideo/
  
