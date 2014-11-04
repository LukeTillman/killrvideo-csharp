using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Data.Videos
{
    public interface IVideoReadModel
    {
        /// <summary>
        /// Gets the details of a specific video.
        /// </summary>
        Task<VideoDetails> GetVideo(Guid videoId);

        /// <summary>
        /// Gets a limited number of video preview data by video id.
        /// </summary>
        Task<IEnumerable<VideoPreview>> GetVideoPreviews(ISet<Guid> videoIds);

        /// <summary>
        /// Gets the current rating stats for the specified video.
        /// </summary>
        Task<VideoRating> GetRating(Guid videoId);

        /// <summary>
        /// Gets the rating given by a user for a specific video.  Will return 0 for the rating if the user hasn't rated the video.
        /// </summary>
        Task<UserVideoRating> GetRatingFromUser(Guid videoId, Guid userId);

        /// <summary>
        /// Gets the latest videos added to the site.
        /// </summary>
        Task<LatestVideos> GetLastestVideos(GetLatestVideos getVideos);

        /// <summary>
        /// Gets a page of videos for a particular user.
        /// </summary>
        Task<UserVideos> GetUserVideos(GetUserVideos getVideos);

        /// <summary>
        /// Gets the first 5 videos related to the specified video.
        /// </summary>
        Task<RelatedVideos> GetRelatedVideos(Guid videoId);

        /// <summary>
        /// Gets a page of videos by tag.
        /// </summary>
        Task<VideosByTag> GetVideosByTag(GetVideosByTag getVideos);

        /// <summary>
        /// Gets a list of tags starting with specified text.
        /// </summary>
        Task<TagsStartingWith> GetTagsStartingWith(GetTagsStartingWith getTags);
    }
}