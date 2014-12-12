using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Models.Upload;
using KillrVideo.Uploads;
using KillrVideo.Uploads.Dtos;
using KillrVideo.VideoCatalog;
using KillrVideo.VideoCatalog.Dtos;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller handles upload of videos.
    /// </summary>
    public class UploadController : ConventionControllerBase
    {
        private readonly IUploadsService _uploads;
        private readonly IVideoCatalogService _videoCatalog;

        public UploadController(IUploadsService uploads, IVideoCatalogService videoCatalog)
        {
            if (uploads == null) throw new ArgumentNullException("uploads");
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            _uploads = uploads;
            _videoCatalog = videoCatalog;
        }

        /// <summary>
        /// Generates a new upload destination in Azure Media Services for the file and returns the URL where the file can be
        /// directly uploaded.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> GenerateUploadDestination(GenerateUploadDestinationViewModel model)
        {
            // Generate a destination for the upload
            UploadDestination uploadDestination = await _uploads.GenerateUploadDestination(new GenerateUploadDestination { FileName = model.FileName });
            if (uploadDestination.ErrorMessage != null)
            {
                ModelState.AddModelError(string.Empty, uploadDestination.ErrorMessage);
                return JsonFailure();
            }
            
            // Return the Id and the URL where the file can be uploaded
            return JsonSuccess(new UploadDestinationViewModel
            {
                UploadUrl = uploadDestination.UploadUrl
            });
        }

        /// <summary>
        /// Adds a new uploaded video.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> Add(AddUploadedVideoViewModel model)
        {
            // Add the uploaded video
            var videoId = Guid.NewGuid();
            var tags = model.Tags == null
                           ? new HashSet<string>()
                           : new HashSet<string>(model.Tags.Select(t => t.Trim()));

            await _videoCatalog.SubmitUploadedVideo(new SubmitUploadedVideo
            {
                UploadUrl = model.UploadUrl,
                VideoId = videoId,
                UserId = User.GetCurrentUserId().Value,
                Name = model.Name,
                Description = model.Description,
                Tags = tags
            });

            // Return a URL where the video can be viewed (after the encoding task is finished)
            return JsonSuccess(new UploadedVideoAddedViewModel
            {
                ViewVideoUrl = Url.Action("View", "Videos", new { videoId })
            });
        }

        /// <summary>
        /// Gets the latest status update for an uploaded video that's being processed.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> GetLatestStatus(GetLatestStatusViewModel model)
        {
            EncodingJobProgress status = await _uploads.GetStatusForVideo(model.VideoId);

            // If there isn't a status (yet) just return queued with a timestamp from 30 seconds ago
            if (status == null)
                return JsonSuccess(new LatestStatusViewModel {Status = "Queued", StatusDate = DateTimeOffset.UtcNow.AddSeconds(-30)});

            return JsonSuccess(new LatestStatusViewModel {StatusDate = status.StatusDate, Status = status.CurrentState});
        }
    }
}