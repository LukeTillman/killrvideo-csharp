using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Models.YouTube;
using KillrVideo.VideoCatalog.Messages;
using KillrVideo.VideoCatalog.Messages.Commands;
using Nimbus;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller that handles functions related to YouTube videos.
    /// </summary>
    public class YouTubeController : ConventionControllerBase
    {
        private readonly IBus _bus;

        public YouTubeController(IBus bus)
        {
            if (bus == null) throw new ArgumentNullException("bus");
            _bus = bus;
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

            await _bus.Send(new SubmitYouTubeVideo
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