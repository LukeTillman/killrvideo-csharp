using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Data.Upload;
using KillrVideo.Data.Upload.Dtos;
using KillrVideo.Data.Videos;
using KillrVideo.Data.Videos.Dtos;
using KillrVideo.Models.Upload;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace KillrVideo.Controllers
{
// ReSharper disable ReplaceWithSingleCallToFirstOrDefault

    /// <summary>
    /// Controller handles upload of videos.
    /// </summary>
    public class UploadController : ConventionControllerBase
    {
        private static readonly TimeSpan UploadMaxTime = TimeSpan.FromHours(8);

        private readonly CloudMediaContext _cloudMediaContext;
        private readonly INotificationEndPoint _notificationEndPoint;
        private readonly IUploadedVideosWriteModel _uploadWriteModel;

        public UploadController(CloudMediaContext cloudMediaContext, INotificationEndPoint notificationEndPoint, IUploadedVideosWriteModel uploadWriteModel)
        {
            if (cloudMediaContext == null) throw new ArgumentNullException("cloudMediaContext");
            if (notificationEndPoint == null) throw new ArgumentNullException("notificationEndPoint");
            if (uploadWriteModel == null) throw new ArgumentNullException("uploadWriteModel");
            _cloudMediaContext = cloudMediaContext;
            _notificationEndPoint = notificationEndPoint;
            _uploadWriteModel = uploadWriteModel;
        }
        
        /// <summary>
        /// Creates a new video Asset in Azure Media services and returns the information necessary for the client to upload the file
        /// directly to the Azure storage account associated with Media Services.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> CreateAsset(CreateAssetViewModel model)
        {
            // TODO:  Validate file type?  Sanitize file name?
            string fileName = model.FileName;

            // Create the media services asset
            string assetName = string.Format("Original - {0}", fileName);
            IAsset asset = await _cloudMediaContext.Assets.CreateAsync(assetName, AssetCreationOptions.None, CancellationToken.None);
            IAssetFile file = await asset.AssetFiles.CreateAsync(fileName, CancellationToken.None);
            
            // Create locator for the upload directly to storage
            ILocator uploadLocator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.Sas, asset, AccessPermissions.Write,
                                                                                   UploadMaxTime, DateTime.UtcNow.AddMinutes(-2));
            
            var uploadUrl = new UriBuilder(uploadLocator.Path);
            uploadUrl.Path = uploadUrl.Path + "/" + fileName;

            // Return the Id and the URL where the file can be uploaded
            return JsonSuccess(new AssetCreatedViewModel
            {
                AssetId = asset.Id,
                FileName = fileName,
                UploadUrl = uploadUrl.Uri.AbsoluteUri,
                UploadLocatorId = uploadLocator.Id
            });
        }

        /// <summary>
        /// Adds a new uploaded video.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> Add(AddUploadedVideoViewModel model)
        {
            // Find the asset to be published
            IAsset asset = _cloudMediaContext.Assets.Where(a => a.Id == model.AssetId).FirstOrDefault();
            if (asset == null)
                throw new InvalidOperationException(string.Format("Could not find asset {0} for publishing.", model.AssetId));

            // TODO:  Validate file size (type again?)

            // Set the file as the primary asset file
            IAssetFile assetFile = asset.AssetFiles.Where(f => f.Name == model.FileName).FirstOrDefault();
            if (assetFile == null)
                throw new InvalidOperationException(string.Format("Could not find file {0} on asset {1}.", model.FileName, model.AssetId));

            assetFile.IsPrimary = true;
            await assetFile.UpdateAsync();
            
            // Remove the upload locator (i.e. revoke upload access)
            ILocator uploadLocator = asset.Locators.Where(l => l.Id == model.UploadLocatorId).FirstOrDefault();
            if (uploadLocator != null)
                await uploadLocator.DeleteAsync();

            // Create a job with a single task to encode the video
            string outputAssetName = string.Format("Encoding - {0}", model.FileName);
            IJob job = _cloudMediaContext.Jobs.CreateWithSingleTask(MediaProcessorNames.WindowsAzureMediaEncoder,
                                                                    MediaEncoderTaskPresetStrings.H264AdaptiveBitrateMP4Set720p, asset,
                                                                    outputAssetName, AssetCreationOptions.None);
            
            // TODO:  Thumbnail creation

            // Get status upades on the job's progress on Azure queue, then start the job
            job.JobNotificationSubscriptions.AddNew(NotificationJobState.All, _notificationEndPoint);
            await job.SubmitAsync();

            // Create record for uploaded video in Cassandra
            var videoId = Guid.NewGuid();
            var tags = model.Tags == null
                           ? new HashSet<string>()
                           : new HashSet<string>(model.Tags.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()));

            await _uploadWriteModel.AddVideo(new AddUploadedVideo
            {
                VideoId = videoId,
                UserId = User.GetCurrentUserId().Value,
                Name = model.Name,
                Description = model.Description,
                Tags = tags,
                JobId = job.Id
            });

            // Return a URL where the video can be viewed (after the encoding task is finished)
            return JsonSuccess(new UploadedVideoAddedViewModel
            {
                ViewVideoUrl = Url.Action("ViewVideo", "Videos", new { videoId })
            });
        }
    }

// ReSharper restore ReplaceWithSingleCallToFirstOrDefault
}