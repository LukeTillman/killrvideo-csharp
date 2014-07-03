using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionFilters;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Data;
using KillrVideo.Data.Videos;
using KillrVideo.Data.Videos.Dtos;
using KillrVideo.Models.Videos;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller for viewing videos.
    /// </summary>
    public class VideosController : ConventionControllerBase
    {
        private readonly IVideoReadModel _videoReadModel;
        private readonly IVideoWriteModel _videoWriteModel;

        public VideosController(IVideoReadModel videoReadModel, IVideoWriteModel videoWriteModel)
        {
            _videoReadModel = videoReadModel;
            _videoWriteModel = videoWriteModel;
        }

        /// <summary>
        /// Shows the View for viewing a specific video.
        /// </summary>
        [HttpGet]
        public async Task<ViewResult> ViewVideo(Guid videoId)
        {
            VideoDetails videoDetails = await _videoReadModel.GetVideo(videoId);
            return View(new ViewVideoViewModel
            {
                VideoId = videoId,
                Title = videoDetails.Name,
                Description = videoDetails.Description,
                LocationType = videoDetails.LocationType,
                Location = videoDetails.Location,
                Tags = videoDetails.Tags,
                UploadDate = videoDetails.AddedDate,
            });
        }

        /// <summary>
        /// Shows the View for adding a new video.
        /// </summary>
        [HttpGet, Authorize]
        public ActionResult Add()
        {
            return View();
        }

        /// <summary>
        /// Saves a new video.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> AddVideo(NewVideoViewModel model)
        {
            // TODO: Add validation
            if (ModelState.IsValid == false)
                return JsonFailure();

            VideoLocationType locationType;
            if (Enum.TryParse(model.LocationType, true, out locationType) == false)
                throw new InvalidOperationException(string.Format("Unknown location type {0}", model.LocationType));
            
            // Assign a Guid to the video and save
            var videoId = Guid.NewGuid();
            var addVideo = new AddVideo
            {
                VideoId = videoId,
                UserId = User.GetCurrentUserId().Value,
                Name = model.Name,
                Description = model.Description,
                Location = model.Location,
                LocationType = locationType,
                Tags = new HashSet<string>(model.Tags.Split(new []{","}, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()))
            };
            await _videoWriteModel.AddVideo(addVideo);

            // Indicate success
            return JsonSuccess(new NewVideoAddedViewModel
            {
                ViewVideoUrl = Url.Action("ViewVideo", "Videos", new {videoId})
            });
        }
        
        /// <summary>
        /// Gets related videos for the video specified in the model.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Related(GetRelatedVideosViewModel model)
        {
            RelatedVideos relatedVideos = await _videoReadModel.GetRelatedVideos(model.VideoId);
            return JsonSuccess(new RelatedVideosViewModel
            {
                Videos = relatedVideos.Videos.Select(VideoPreviewViewModel.FromDataModel).ToList()
            });
        }

        /// <summary>
        /// Gets videos for the specified user or the currently logged in user if one is not specified.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> ByUser(GetUserVideosViewModel model)
        {
            // If no user was specified, default to the current logged in user
            Guid? userId = model.UserId ?? User.GetCurrentUserId();
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "No user specified and no user currently logged in.");
                return JsonFailure();
            }

            UserVideos userVideos = await _videoReadModel.GetUserVideos(new GetUserVideos
            {
                UserId = userId.Value,
                PageSize = model.PageSize,
                FirstVideoOnPageAddedDate = model.FirstVideoOnPage == null ? (DateTimeOffset?) null : model.FirstVideoOnPage.AddedDate,
                FirstVideoOnPageVideoId = model.FirstVideoOnPage == null ? (Guid?) null : model.FirstVideoOnPage.VideoId
            });

            return JsonSuccess(new UserVideosViewModel
            {
                UserId = userVideos.UserId,
                Videos = userVideos.Videos.Select(VideoPreviewViewModel.FromDataModel).ToList()
            });
        }

        /// <summary>
        /// Gets the most recent videos.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Recent(GetRecentVideosViewModel model)
        {
            LatestVideos recentVideos = await _videoReadModel.GetLastestVideos(model.PageSize);
            return JsonSuccess(new RecentVideosViewModel
            {
                Videos = recentVideos.Videos.Select(VideoPreviewViewModel.FromDataModel).ToList()
            });
        }

        /// <summary>
        /// Get the ratings data for a video.
        /// </summary>
        [HttpGet, NoCache]
        public async Task<JsonNetResult> GetRatings(GetRatingsViewModel model)
        {
            // We definitely want the overall rating info, so start there
            Task<VideoRating> ratingTask = _videoReadModel.GetRating(model.VideoId);

            // If a user is logged in, we also want their rating
            Guid? userId = User.GetCurrentUserId();
            Task<UserVideoRating> userRatingTask = null;
            if (userId.HasValue)
                userRatingTask = _videoReadModel.GetRatingFromUser(model.VideoId, userId.Value);

            // Await data appropriately
            VideoRating ratingData = await ratingTask;
            UserVideoRating userRating = null;
            if (userRatingTask != null)
                userRating = await userRatingTask;

            return JsonSuccess(new RatingsViewModel
            {
                VideoId = ratingData.VideoId,
                CurrentUserLoggedIn = userId.HasValue,
                CurrentUserRating = userRating == null ? 0 : userRating.Rating,
                RatingsCount = ratingData.RatingsCount,
                RatingsSum = ratingData.RatingsTotal
            });
        }

        /// <summary>
        /// Rates a video.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> Rate(RateVideoViewModel model)
        {
            await _videoWriteModel.RateVideo(new RateVideo
            {
                VideoId = model.VideoId, 
                UserId = User.GetCurrentUserId().Value,
                Rating = model.Rating
            });
            return JsonSuccess();
        }
	}
}