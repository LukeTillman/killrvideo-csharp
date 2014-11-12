using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Comments.Api.Commands;
using KillrVideo.VideoCatalog.Api.Commands;
using NLipsum.Core;

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

            // Insert comments for each video
            var random = new Random();
            var commentWriteModel = new CommentWriteModel(session);
            var commentTasks = new List<Task>();
            var lipsum = new LipsumGenerator(Lipsums.ChildHarold, false);

            foreach (AddVideo video in SampleVideos.Data)
            {
                int commentsToCreate = random.Next(1, 101);
                for (int i = 0; i < commentsToCreate; i++)
                {
                    Guid userId = SampleUsers.Data[random.Next(SampleUsers.Data.Length)].UserId;
                    commentTasks.Add(commentWriteModel.CommentOnVideo(new CommentOnVideo
                    {
                        UserId = userId,
                        VideoId = video.VideoId,
                        CommentTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(random.Next(14))),
                        Comment = lipsum.GenerateParagraphs(1, Paragraph.Short)[0]
                    }));
                }
            }

            await Task.WhenAll(commentTasks);

            // Insert ratings for videos
            
        }
    }
}
