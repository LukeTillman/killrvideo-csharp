using System;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog.Messages.Commands;

namespace KillrVideo.VideoCatalog
{
    /// <summary>
    /// Writes video catalog data to Cassandra.
    /// </summary>
    public class VideoCatalogWriteModel : IVideoCatalogWriteModel
    {
        private static readonly int LatestVideosTtlSeconds =
            Convert.ToInt32(TimeSpan.FromDays(VideoCatalogReadModel.MaxDaysInPastForLatestVideos).TotalSeconds);

        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement[]> _addVideoStatements;

        public VideoCatalogWriteModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            // Multiple statement are involved in adding a video so wrap those in a single AsyncLazy so we can await all of
            // them being prepared
            _addVideoStatements = new AsyncLazy<PreparedStatement[]>(() => Task.WhenAll(new[]
            {
                _session.PrepareAsync("INSERT INTO videos (videoid, userid, name, description, location, location_type, " +
                                      "preview_image_location, tags, added_date) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)"),

                _session.PrepareAsync("INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) " +
                                      "VALUES (?, ?, ?, ?, ?)"),

                // Use TTL based on how far back we'll be querying for latest videos
                _session.PrepareAsync(
                    string.Format("INSERT INTO latest_videos (yyyymmdd, added_date, videoid, userid, name, preview_image_location) " +
                                  "VALUES (?, ?, ?, ?, ?, ?) USING TTL {0}", LatestVideosTtlSeconds)),

                _session.PrepareAsync("INSERT INTO videos_by_tag (tag, videoid, added_date, userid, name, preview_image_location, tagged_date) " +
                                      "VALUES (?, ?, ?, ?, ?, ?, ?)"),

                _session.PrepareAsync("INSERT INTO tags_by_letter (first_letter, tag) VALUES (?, ?)")
            }));
        }

        /// <summary>
        /// Adds a new video.
        /// </summary>
        public async Task AddVideo(AddVideo video)
        {
            // Calculate date-related data for the video
            DateTimeOffset addDate = DateTimeOffset.UtcNow;
            string yyyymmdd = addDate.ToString("yyyyMMdd");

            // Use an atomic batch to send over all the inserts
            var batchStatement = new BatchStatement();

            // Await to make sure all statements are prepared, then bind values for each statement (see the PrepareAddVideoStatements method
            // for the actual CQL and the order of the statements)
            PreparedStatement[] preparedStatements = await _addVideoStatements;

            // INSERT INTO videos
            batchStatement.Add(preparedStatements[0].Bind(video.VideoId, video.UserId, video.Name, video.Description, video.Location,
                                                          (int) video.LocationType, video.PreviewImageLocation, video.Tags, addDate));

            // INSERT INTO user_videos
            batchStatement.Add(preparedStatements[1].Bind(video.UserId, addDate, video.VideoId, video.Name, video.PreviewImageLocation));

            // INSERT INTO latest_videos
            batchStatement.Add(preparedStatements[2].Bind(yyyymmdd, addDate, video.VideoId, video.UserId, video.Name, video.PreviewImageLocation));

            // We need to add multiple statements for each tag
            foreach (string tag in video.Tags)
            {
                // INSERT INTO videos_by_tag
                batchStatement.Add(preparedStatements[3].Bind(tag, video.VideoId, addDate, video.UserId, video.Name, video.PreviewImageLocation, addDate));

                // INSERT INTO tags_by_letter
                string firstLetter = tag.Substring(0, 1);
                batchStatement.Add(preparedStatements[4].Bind(firstLetter, tag));
            }

            // Send the batch
            await _session.ExecuteAsync(batchStatement).ConfigureAwait(false);
        }
    }
}