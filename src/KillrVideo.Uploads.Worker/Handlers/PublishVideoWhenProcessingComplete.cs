using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.Messages.Events;
using KillrVideo.Utils;
using Microsoft.WindowsAzure.MediaServices.Client;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.Uploads.Worker.Handlers
{
    /// <summary>
    /// Publishes a video for playback when the encoding job is complete.
    /// </summary>
    public class PublishVideoWhenProcessingComplete : IHandleCompetingEvent<UploadedVideoProcessingSucceeded>
    {
        private static readonly TimeSpan PublishedVideosGoodFor = TimeSpan.FromDays(10000);

        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;
        private readonly CloudMediaContext _cloudMediaContext;

        private readonly Random _random;

        public PublishVideoWhenProcessingComplete(ISession session, TaskCache<string, PreparedStatement> statementCache,
                                                  IBus bus, CloudMediaContext cloudMediaContext)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            if (cloudMediaContext == null) throw new ArgumentNullException("cloudMediaContext");
            _session = session;
            _statementCache = statementCache;
            _bus = bus;
            _cloudMediaContext = cloudMediaContext;

            _random = new Random();
        }

        // ReSharper disable ReplaceWithSingleCallToFirstOrDefault
        // ReSharper disable ReplaceWithSingleCallToSingleOrDefault

        public async Task Handle(UploadedVideoProcessingSucceeded encodedVideo)
        {
            // Lookup the job Id for the video in Cassandra
            PreparedStatement lookupPrepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT jobid FROM uploaded_video_jobs WHERE videoid = ?");
            RowSet lookupRows = await _session.ExecuteAsync(lookupPrepared.Bind(encodedVideo.VideoId));
            Row lookupRow = lookupRows.SingleOrDefault();
            if (lookupRow == null)
                throw new InvalidOperationException(string.Format("Could not find job for video id {0}", encodedVideo.VideoId));

            var jobId = lookupRow.GetValue<string>("jobid");

            // Find the job in Azure Media Services and throw if not found
            IJob job = _cloudMediaContext.Jobs.Where(j => j.Id == jobId).SingleOrDefault();
            if (job == null)
                throw new InvalidOperationException(string.Format("Could not find job {0}", jobId));

            List<IAsset> outputAssets = job.OutputMediaAssets.ToList();

            // Find the encoded video asset
            IAsset asset = outputAssets.SingleOrDefault(a => a.Name.StartsWith(UploadConfig.EncodedVideoAssetNamePrefix));
            if (asset == null)
                throw new InvalidOperationException(string.Format("Could not find video output asset for job {0}", jobId));

            // Publish the asset for progressive downloading (HTML5) by creating an SAS locator for it and adding the file name to the path
            ILocator locator = asset.Locators.Where(l => l.Type == LocatorType.Sas).FirstOrDefault();
            if (locator == null)
            {
                const AccessPermissions readPermissions = AccessPermissions.Read | AccessPermissions.List;
                locator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.Sas, asset, readPermissions,
                                                                        PublishedVideosGoodFor);
            }

            // Get the URL for streaming from the locator (embed file name for the mp4 in locator before query string)
            IAssetFile mp4File = asset.AssetFiles.ToList().Single(f => f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));
            var videoLocation = new UriBuilder(locator.Path);
            videoLocation.Path += "/" + mp4File.Name;

            // Find the thumbnail asset
            IAsset thumbnailAsset = outputAssets.SingleOrDefault(a => a.Name.StartsWith(UploadConfig.ThumbnailAssetNamePrefix));
            if (thumbnailAsset == null)
                throw new InvalidOperationException(string.Format("Could not find thumbnail output asset for job {0}", jobId));

            // Publish the thumbnail asset by creating a locator for it (again, check if already present)
            ILocator thumbnailLocator = thumbnailAsset.Locators.Where(l => l.Type == LocatorType.Sas).FirstOrDefault();
            if (thumbnailLocator == null)
            {
                thumbnailLocator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.Sas, thumbnailAsset, AccessPermissions.Read,
                                                                                 PublishedVideosGoodFor);
            }

            // Get the URL for a random thumbnail file in the asset
            List<IAssetFile> jpgFiles =
                thumbnailAsset.AssetFiles.ToList().Where(f => f.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToList();
            var thumbnailLocation = new UriBuilder(thumbnailLocator.Path);
            int randomThumbnailIndex = _random.Next(jpgFiles.Count);
            thumbnailLocation.Path += "/" + jpgFiles[randomThumbnailIndex].Name;

            // Tell the world about the video that's been published
            await _bus.Publish(new UploadedVideoPublished
            {
                VideoId = encodedVideo.VideoId,
                VideoUrl = videoLocation.Uri.AbsoluteUri,
                ThumbnailUrl = thumbnailLocation.Uri.AbsoluteUri,
                Timestamp = encodedVideo.Timestamp
            });
        }

        // ReSharper restore ReplaceWithSingleCallToFirstOrDefault
        // ReSharper restore ReplaceWithSingleCallToSingleOrDefault
    }
}
