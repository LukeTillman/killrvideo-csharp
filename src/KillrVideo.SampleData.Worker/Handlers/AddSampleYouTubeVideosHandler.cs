using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KillrVideo.SampleData.Dtos;
using KillrVideo.SampleData.Worker.Components;
using KillrVideo.SampleData.Worker.Components.YouTube;
using KillrVideo.VideoCatalog;
using KillrVideo.VideoCatalog.Dtos;
using Nimbus.Handlers;

namespace KillrVideo.SampleData.Worker.Handlers
{
    /// <summary>
    /// Adds sample YouTube videos to the site.
    /// </summary>
    public class AddSampleYouTubeVideosHandler : IHandleCommand<AddSampleYouTubeVideos>
    {
        private readonly IGetSampleData _sampleDataRetriever;
        private readonly IManageSampleYouTubeVideos _youTubeManager;
        private readonly IVideoCatalogService _videoCatalog;

        public AddSampleYouTubeVideosHandler(IGetSampleData sampleDataRetriever, IManageSampleYouTubeVideos youTubeManager, 
                                             IVideoCatalogService videoCatalog)
        {
            if (sampleDataRetriever == null) throw new ArgumentNullException("sampleDataRetriever");
            if (youTubeManager == null) throw new ArgumentNullException("youTubeManager");
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");

            _sampleDataRetriever = sampleDataRetriever;
            _youTubeManager = youTubeManager;
            _videoCatalog = videoCatalog;
        }

        public async Task Handle(AddSampleYouTubeVideos busCommand)
        {
            // Get some sample users to be the authors for the videos we're going to add
            List<Guid> sampleUserIds = await _sampleDataRetriever.GetRandomSampleUserIds(busCommand.NumberOfVideos).ConfigureAwait(false);

            // Get some unused sample videos
            List<YouTubeVideo> sampleVideos = await _youTubeManager.GetUnusedVideos(busCommand.NumberOfVideos).ConfigureAwait(false);

            // Add them to the site using sample users
            for (int idx = 0; idx < sampleVideos.Count; idx++)
            {
                YouTubeVideo sampleVideo = sampleVideos[idx];
                Guid userId = sampleUserIds[idx];
                
                // Submit the video
                await _videoCatalog.SubmitYouTubeVideo(new SubmitYouTubeVideo
                {
                    VideoId = Guid.NewGuid(),
                    UserId = userId,
                    YouTubeVideoId = sampleVideo.YouTubeVideoId,
                    Name = sampleVideo.Name,
                    Description = sampleVideo.Description,
                    Tags = new HashSet<string>()
                }).ConfigureAwait(false);

                // Mark them as used so we make a best effort not to reuse sample videos and post duplicates
                await _youTubeManager.MarkVideoAsUsed(sampleVideo).ConfigureAwait(false);
            }
        }
    }
}