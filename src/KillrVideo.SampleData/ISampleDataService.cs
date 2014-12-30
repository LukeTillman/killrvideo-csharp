using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;

namespace KillrVideo.SampleData
{
    /// <summary>
    /// A service for adding sample data to the site.
    /// </summary>
    public interface ISampleDataService
    {
        /// <summary>
        /// Adds sample comments to the site.
        /// </summary>
        Task AddSampleComments(AddSampleComments comments);

        /// <summary>
        /// Adds sample video ratings to the site.
        /// </summary>
        Task AddSampleRatings(AddSampleRatings ratings);

        /// <summary>
        /// Adds sample users to the site.
        /// </summary>
        Task AddSampleUsers(AddSampleUsers users);

        /// <summary>
        /// Adds sample video views to the site.
        /// </summary>
        Task AddSampleVideoViews(AddSampleVideoViews views);

        /// <summary>
        /// Adds sample YouTube videos to the site.
        /// </summary>
        Task AddSampleYouTubeVideos(AddSampleYouTubeVideos videos);
    }
}
