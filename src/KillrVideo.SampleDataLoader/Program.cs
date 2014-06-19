using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Data.Users;
using KillrVideo.Data.Videos;

namespace KillrVideo.SampleDataLoader
{
    /// <summary>
    /// A console app that will write sample data to the killrvideo keyspace.
    /// </summary>
    class Program
    {
        private const string ClusterLocationAppSettingsKey = "CassandraClusterLocation";
        private const string Keyspace = "killrvideo";

        static void Main(string[] args)
        {
            // Get cluster IP/host and keyspace from .config file
            string clusterLocation = ConfigurationManager.AppSettings[ClusterLocationAppSettingsKey];

            // Use the Cluster builder to create a cluster
            Cluster cluster = Cluster.Builder().AddContactPoint(clusterLocation).Build();

            // Now connect to the keyspace and insert the sample data
            using (ISession session = cluster.Connect(Keyspace))
            {
                // Insert sample data
                InsertSampleData(session).Wait();
            }
        }

        private static async Task InsertSampleData(ISession session)
        {
            // Insert users
            var userWriteModel = new UserWriteModel(session);
            var userTasks = SampleUsers.Data.Select(userWriteModel.CreateUser).Cast<Task>().ToList();
            await Task.WhenAll(userTasks);

            // Insert videos
            var videoWriteModel = new VideoWriteModel(session);
            var videoTasks = SampleVideos.Data.Select(videoWriteModel.AddVideo).ToList();
            await Task.WhenAll(videoTasks);

            // Insert comments
        }
    }
}
