using System.Collections.Generic;
using System.Threading.Tasks;

namespace KillrVideo.SampleData.Worker.Components.YouTube
{
    /// <summary>
    /// Component responsible for managing sample YouTube videos that can be added to the site.
    /// </summary>
    public interface IManageSampleYouTubeVideos
    {
        /// <summary>
        /// Refreshes the videos cached in Cassandra for the given source.
        /// </summary>
        Task RefreshSource(YouTubeVideoSource source);

        /// <summary>
        /// Gets a list of unused YouTubeVideos with the page size specified.
        /// </summary>
        Task<List<YouTubeVideo>> GetUnusedVideos(int pageSize);

        /// <summary>
        /// Marks the YouTube video specified as used.
        /// </summary>
        Task MarkVideoAsUsed(YouTubeVideo video);
    }
}