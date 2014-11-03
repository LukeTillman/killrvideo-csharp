using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Data;
using KillrVideo.Data.Videos;
using KillrVideo.Data.Videos.Dtos;
using KillrVideo.Models.YouTube;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller that handles functions related to YouTube videos.
    /// </summary>
    public class YouTubeController : ConventionControllerBase
    {
        private readonly IVideoWriteModel _videoWriteModel;

        public YouTubeController(IVideoWriteModel videoWriteModel)
        {
            if (videoWriteModel == null) throw new ArgumentNullException("videoWriteModel");
            _videoWriteModel = videoWriteModel;
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

            var addVideo = new AddVideo
            {
                VideoId = videoId,
                UserId = User.GetCurrentUserId().Value,
                Name = model.Name,
                Description = model.Description,
                Location = model.YouTubeVideoId,
                LocationType = VideoLocationType.YouTube,
                Tags = tags,
                PreviewImageLocation = string.Format("//img.youtube.com/vi/{0}/hqdefault.jpg", model.YouTubeVideoId)
            };
            await _videoWriteModel.AddVideo(addVideo);

            // Indicate success
            return JsonSuccess(new YouTubeVideoAddedViewModel
            {
                ViewVideoUrl = Url.Action("View", "Videos", new { videoId })
            });
        }
    }
}