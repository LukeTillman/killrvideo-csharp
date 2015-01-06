using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Models.YouTube;
using KillrVideo.VideoCatalog;
using KillrVideo.VideoCatalog.Dtos;
using Microsoft.WindowsAzure;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller that handles functions related to YouTube videos.
    /// </summary>
    public class YouTubeController : ConventionControllerBase
    {
        private readonly IVideoCatalogService _videoCatalog;

        public YouTubeController(IVideoCatalogService videoCatalog)
        {
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            _videoCatalog = videoCatalog;
        }

        /// <summary>
        /// Gets the YouTube API key.
        /// </summary>
        [HttpGet]
        public JsonNetResult GetKey()
        {
            return JsonSuccess(new GetKeyViewModel
            {
                YouTubeApiKey = CloudConfigurationManager.GetSetting("YouTubeApiKey")
            });
        }

        /// <summary>
        /// Handles adding a new YouTube video.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> Add(AddYouTubeVideoViewModel model)
        {
            // TODO: Add validation
            if (ModelState.IsValid == false)
                return JsonFailure();

            // Assign a Guid to the video and save
            var videoId = Guid.NewGuid();
            var tags = model.Tags == null
                           ? new HashSet<string>()
                           : new HashSet<string>(model.Tags.Select(t => t.Trim()));

            await _videoCatalog.SubmitYouTubeVideo(new SubmitYouTubeVideo
            {
                VideoId = videoId,
                UserId = User.GetCurrentUserId().Value,
                Name = model.Name,
                Description = model.Description,
                Tags = tags,
                YouTubeVideoId = model.YouTubeVideoId
            });
            
            // Indicate success
            return JsonSuccess(new YouTubeVideoAddedViewModel
            {
                ViewVideoUrl = Url.Action("View", "Videos", new { videoId })
            });
        }
    }
}