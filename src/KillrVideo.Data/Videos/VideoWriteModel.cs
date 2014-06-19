using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Data.Videos
{
    /// <summary>
    /// Handles writes/updates for videos.
    /// </summary>
    public class VideoWriteModel : IVideoWriteModel
    {
        private readonly ISession _session;

        private readonly AsyncLazy<PreparedStatement[]> _addVideoStatements;
        private readonly AsyncLazy<PreparedStatement[]> _renameVideoStatements;
        private readonly AsyncLazy<PreparedStatement[]> _addRatingStatements;

        private readonly AsyncLazy<PreparedStatement> _changeDescription;
        private readonly AsyncLazy<PreparedStatement> _getDetailsForRename; 
        
        public VideoWriteModel(ISession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;

            // Multiple statement are involved in adding/renaming a video so wrap those in a single AsyncLazy so we can await all of
            // them being prepared
            _addVideoStatements = new AsyncLazy<PreparedStatement[]>(PrepareAddVideoStatements);
            _renameVideoStatements = new AsyncLazy<PreparedStatement[]>(PrepareRenameVideoStatements);
            _addRatingStatements = new AsyncLazy<PreparedStatement[]>(PrepareAddRatingStatements);

            // Some other reusable PreparedStatements
            _changeDescription = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "UPDATE videos SET description = ? WHERE videoid = ?"));

            _getDetailsForRename = new AsyncLazy<PreparedStatement>(() => _session.PrepareAsync(
                "SELECT userid, added_date, tags FROM videos WHERE videoid = ?"));
        }

        /// <summary>
        /// Adds a new video.
        /// </summary>
        public async Task AddVideo(AddVideo video)
        {
            // Calculate date-related data for the video
            DateTimeOffset addDate = DateTimeOffset.UtcNow;
            string yyyymmdd = addDate.ToString("yyyyMMdd");

            string previewImage = GetPreviewImageLocation(video);
            
            // Use an atomic batch to send over all the inserts
            var batchStatement = new BatchStatement();
            
            // Await to make sure all statements are prepared, then bind values for each statement (see the PrepareAddVideoStatements method
            // for the actual CQL and the order of the statements)
            PreparedStatement[] preparedStatements = await _addVideoStatements;

            // INSERT INTO videos
            batchStatement.AddQuery(preparedStatements[0].Bind(video.VideoId, video.UserId, video.Name, video.Description, video.Location,
                                                               (int) video.LocationType, previewImage, video.Tags, addDate));

            // INSERT INTO user_videos
            batchStatement.AddQuery(preparedStatements[1].Bind(video.UserId, addDate, video.VideoId, video.Name, previewImage));

            // INSERT INTO latest_videos
            batchStatement.AddQuery(preparedStatements[2].Bind(yyyymmdd, addDate, video.VideoId, video.Name, previewImage));
            
            // We need to add multiple statements for each tag
            foreach (string tag in video.Tags)
            {
                // INSERT INTO videos_by_tag
                batchStatement.AddQuery(preparedStatements[3].Bind(tag, video.VideoId, addDate, video.Name, previewImage, addDate));

                // INSERT INTO tags_by_letter
                string firstLetter = tag.Substring(0, 1);
                batchStatement.AddQuery(preparedStatements[4].Bind(firstLetter, tag));
            }

            // Send the batch
            await _session.ExecuteAsync(batchStatement).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a rating for a video.
        /// </summary>
        public async Task RateVideo(RateVideo videoRating)
        {
            PreparedStatement[] preparedStatements = await _addRatingStatements;

            // We can't use a batch here because we can't mix counters with regular DML, but we can run both of them at the same time
            await _session.ExecuteMultipleAsync(
                // UPDATE video_ratings... (Driver will throw if we don't cast the rating to a long I guess because counters are data type long)
                preparedStatements[0].Bind((long) videoRating.Rating, videoRating.VideoId),
                // INSERT INTO video_ratings_by_user
                preparedStatements[1].Bind(videoRating.VideoId, videoRating.UserId, videoRating.Rating)
            );
        }

        /// <summary>
        /// Renames a video.
        /// </summary>
        public async Task RenameVideo(RenameVideo renameVideo)
        {
            // We'll need to know the user id and added date for the video (which are set on the initial insert 
            // and then don't change) in order to update the name in all the appropriate places
            PreparedStatement getPreparedStatement = await _getDetailsForRename;
            BoundStatement getStatement = getPreparedStatement.Bind(renameVideo.VideoId);
            RowSet rows = await _session.ExecuteAsync(getStatement).ConfigureAwait(false);
            Row result = rows.Single();
            var userid = result.GetValue<Guid>("userid");
            var addedDate = result.GetValue<DateTimeOffset>("added_date");
            string yyyymmdd = addedDate.ToString("yyyyMMdd");
            var tags = result.GetValue<IEnumerable<string>>("tags");
            
            // Get the prepared statements needed to rename the video
            PreparedStatement[] preparedStatements = await _renameVideoStatements;

            // Use a batch and bind values for each statement (see PrepareRenameVideoStatements for the CQL and order of the statements)
            var batch = new BatchStatement();

            // UPDATE videos ...
            batch.AddQuery(preparedStatements[0].Bind(renameVideo.Name, renameVideo.VideoId));
            // UPDATE user_videos ...
            batch.AddQuery(preparedStatements[1].Bind(renameVideo.Name, userid, addedDate, renameVideo.VideoId));
            // UPDATE latest_videos ...
            batch.AddQuery(preparedStatements[2].Bind(renameVideo.Name, yyyymmdd, addedDate, renameVideo.VideoId));

            // UPDATE videos_by_tag
            foreach (string tag in tags)
                batch.AddQuery(preparedStatements[3].Bind(renameVideo.Name, tag, renameVideo.VideoId));

            // Send the batch
            await _session.ExecuteAsync(batch);
        }

        /// <summary>
        /// Changes a video's description.
        /// </summary>
        public async Task ChangeVideoDescription(ChangeVideoDescription changeVideo)
        {
            PreparedStatement preparedStatement = await _changeDescription;
            BoundStatement boundStatement = preparedStatement.Bind(changeVideo.Description, changeVideo.VideoId);
            await _session.ExecuteAsync(boundStatement);
        }
        
        private Task<PreparedStatement[]> PrepareAddVideoStatements()
        {
            return Task.WhenAll(new[]
            {
                _session.PrepareAsync("INSERT INTO videos (videoid, userid, name, description, location, location_type, " +
                                      "preview_image_location, tags, added_date) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)"),

                _session.PrepareAsync("INSERT INTO user_videos (userid, added_date, videoid, name, preview_image_location) " +
                                      "VALUES (?, ?, ?, ?, ?)"),

                // Use TTL of 7 days since we won't every query back any further than that
                _session.PrepareAsync("INSERT INTO latest_videos (yyyymmdd, added_date, videoid, name, preview_image_location) " +
                                      "VALUES (?, ?, ?, ?, ?) USING TTL 604800"),

                _session.PrepareAsync("INSERT INTO videos_by_tag (tag, videoid, added_date, name, preview_image_location, tagged_date) " +
                                      "VALUES (?, ?, ?, ?, ?, ?)"),

                _session.PrepareAsync("INSERT INTO tags_by_letter (first_letter, tag) VALUES (?, ?)")
            });
        }

        private Task<PreparedStatement[]> PrepareRenameVideoStatements()
        {
            return Task.WhenAll(new[]
            {
                _session.PrepareAsync("UPDATE videos SET name = ? WHERE videoid = ?"),
                _session.PrepareAsync("UPDATE user_videos SET name = ? WHERE userid = ? AND added_date = ? AND videoid = ?"),
                _session.PrepareAsync("UPDATE latest_videos SET name = ? WHERE yyyymmdd = ? AND added_date = ? AND videoid = ?"),
                _session.PrepareAsync("UPDATE videos_by_tag SET name = ? WHERE tag = ? AND videoid = ?")
            });
        }

        private Task<PreparedStatement[]> PrepareAddRatingStatements()
        {
            return Task.WhenAll(new[]
            {
                _session.PrepareAsync("UPDATE video_ratings SET rating_counter = rating_counter + 1, rating_total = rating_total + ? WHERE videoid = ?"),
                _session.PrepareAsync("INSERT INTO video_ratings_by_user (videoid, userid, rating) VALUES (?, ?, ?)")
            });
        }

        private static string GetPreviewImageLocation(AddVideo video)
        {
            switch (video.LocationType)
            {
                case VideoLocationType.YouTube:
                    return string.Format("//img.youtube.com/vi/{0}/hqdefault.jpg", video.Location);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}