using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KillrVideo.Cassandra;
using KillrVideo.Host.Config;
using KillrVideo.Protobuf;

namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Searches the videos_by_tag table to offer suggestions for related videos. Does not support paging currently.
    /// </summary>
    [Export(typeof(IGrpcServerService))]
    public class SuggestVideosByTag : SuggestedVideoService.ISuggestedVideoService, IConditionalGrpcServerService
    {
        private const int RelatedVideosToReturn = 4;

        private readonly ISession _session;
        private readonly PreparedStatementCache _statementCache;

        public SuggestVideosByTag(ISession session, PreparedStatementCache statementCache)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (statementCache == null) throw new ArgumentNullException(nameof(statementCache));
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Convert this instance to a ServerServiceDefinition that can be run on a Grpc server.
        /// </summary>
        public ServerServiceDefinition ToServerServiceDefinition()
        {
            return SuggestedVideoService.BindService(this);
        }

        /// <summary>
        /// Returns true if this service should run given the configuration of the host.
        /// </summary>
        public bool ShouldRun(IHostConfiguration hostConfig)
        {
            // Use this implementation when DSE Search and Spark are not enabled or not present in the host config
            return SuggestionsConfig.UseDse(hostConfig) == false;
        }

        /// <summary>
        /// Gets the first 4 videos related to the specified video. Does not support paging.
        /// </summary>
        public async Task<GetRelatedVideosResponse> GetRelatedVideos(GetRelatedVideosRequest request, ServerCallContext context)
        {
            // Lookup the tags for the video
            PreparedStatement tagsForVideoPrepared = await _statementCache.GetOrAddAsync("SELECT tags FROM videos WHERE videoid = ?");
            BoundStatement tagsForVideoBound = tagsForVideoPrepared.Bind(request.VideoId.ToGuid());
            RowSet tagRows = await _session.ExecuteAsync(tagsForVideoBound).ConfigureAwait(false);

            // Start with an empty response
            var response = new GetRelatedVideosResponse { VideoId = request.VideoId };
            
            Row tagRow = tagRows.SingleOrDefault();
            if (tagRow == null)
                return response;

            var tagsValue = tagRow.GetValue<IEnumerable<string>>("tags");
            var tags = tagsValue?.ToList() ?? new List<string>();

            // If there are no tags, we can't find related videos
            if (tags.Count == 0)
                return response;

            var relatedVideos = new Dictionary<Uuid, SuggestedVideoPreview>();
            PreparedStatement videosForTagPrepared = await _statementCache.GetOrAddAsync("SELECT * FROM videos_by_tag WHERE tag = ? LIMIT ?");

            var inFlightQueries = new List<Task<RowSet>>();
            for (var i = 0; i < tags.Count; i++)
            {
                // Use the number of results we ultimately want * 2 when querying so that we can account for potentially having to filter 
                // out the video Id we're using as the basis for the query as well as duplicates
                const int pageSize = RelatedVideosToReturn * 2;

                // Kick off a query for each tag and track them in the inflight requests list
                string tag = tags[i];
                IStatement query = videosForTagPrepared.Bind(tag, pageSize);
                inFlightQueries.Add(_session.ExecuteAsync(query));

                // Every third query, or if this is the last tag, wait on all the query results
                if (inFlightQueries.Count == 3 || i == tags.Count - 1)
                {
                    RowSet[] results = await Task.WhenAll(inFlightQueries).ConfigureAwait(false);

                    foreach (RowSet rowSet in results)
                    {
                        foreach (Row row in rowSet)
                        {
                            SuggestedVideoPreview preview = MapRowToVideoPreview(row);

                            // Skip self
                            if (preview.VideoId.Equals(request.VideoId))
                                continue;

                            // Skip videos we already have in the results
                            if (relatedVideos.ContainsKey(preview.VideoId))
                                continue;

                            // Add to results
                            relatedVideos.Add(preview.VideoId, preview);

                            // If we've got enough, no reason to continue
                            if (relatedVideos.Count >= RelatedVideosToReturn)
                                break;
                        }

                        // If we've got enough, no reason to continue
                        if (relatedVideos.Count >= RelatedVideosToReturn)
                            break;
                    }

                    // See if we've got enough results now to fulfill our requirement
                    if (relatedVideos.Count >= RelatedVideosToReturn)
                        break;

                    // We don't have enough yet, so reset the inflight requests to allow another batch of tags to be queried
                    inFlightQueries.Clear();
                }
            }

            // Add any videos found to the response and return
            response.Videos.Add(relatedVideos.Values);
            return response;
        }

        /// <summary>
        /// Gets the personalized video suggestions for a specific user.
        /// </summary>
        public Task<GetSuggestedForUserResponse> GetSuggestedForUser(GetSuggestedForUserRequest request, ServerCallContext context)
        {
            // TODO: Can we implement suggestions without DSE and Spark? (Yeah, probably not)
            var response = new GetSuggestedForUserResponse { UserId = request.UserId };
            return Task.FromResult(response);
        }
        
        /// <summary>
        /// Maps a row to a VideoPreview object.
        /// </summary>
        private static SuggestedVideoPreview MapRowToVideoPreview(Row row)
        {
            return new SuggestedVideoPreview
            {
                VideoId = row.GetValue<Guid>("videoid").ToUuid(),
                AddedDate = row.GetValue<DateTimeOffset>("added_date").ToTimestamp(),
                Name = row.GetValue<string>("name"),
                PreviewImageLocation = row.GetValue<string>("preview_image_location"),
                UserId = row.GetValue<Guid>("userid").ToUuid()
            };
        }
    }
}
