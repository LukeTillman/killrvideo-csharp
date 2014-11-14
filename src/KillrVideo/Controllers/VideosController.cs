using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionFilters;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Models.Shared;
using KillrVideo.Models.Videos;
using KillrVideo.Ratings;
using KillrVideo.Ratings.Dtos;
using KillrVideo.Ratings.Messages.Commands;
using KillrVideo.Statistics;
using KillrVideo.Statistics.Dtos;
using KillrVideo.SuggestedVideos;
using KillrVideo.SuggestedVideos.Dtos;
using KillrVideo.Uploads;
using KillrVideo.Uploads.Dtos;
using KillrVideo.UserManagement;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.VideoCatalog;
using KillrVideo.VideoCatalog.Dtos;
using KillrVideo.VideoCatalog.Messages;
using Nimbus;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller for viewing videos.
    /// </summary>
    public class VideosController : ConventionControllerBase
    {
        private readonly IVideoCatalogReadModel _videoReadModel;
        private readonly IUploadedVideosReadModel _uploadReadModel;
        private readonly IUserReadModel _userReadModel;
        private readonly IPlaybackStatsReadModel _statsReadModel;
        private readonly IRatingsReadModel _ratingsReadModel;
        private readonly ISuggestVideos _suggestionService;
        private readonly IBus _bus;

        public VideosController(IVideoCatalogReadModel videoReadModel, IUploadedVideosReadModel uploadReadModel, IUserReadModel userReadModel,
                                IPlaybackStatsReadModel statsReadModel, IRatingsReadModel ratingsReadModel, ISuggestVideos suggestionService, IBus bus)
        {
            if (videoReadModel == null) throw new ArgumentNullException("videoReadModel");
            if (uploadReadModel == null) throw new ArgumentNullException("uploadReadModel");
            if (userReadModel == null) throw new ArgumentNullException("userReadModel");
            if (statsReadModel == null) throw new ArgumentNullException("statsReadModel");
            if (ratingsReadModel == null) throw new ArgumentNullException("ratingsReadModel");
            if (suggestionService == null) throw new ArgumentNullException("suggestionService");
            if (bus == null) throw new ArgumentNullException("bus");

            _videoReadModel = videoReadModel;
            _uploadReadModel = uploadReadModel;
            _userReadModel = userReadModel;
            _statsReadModel = statsReadModel;
            _ratingsReadModel = ratingsReadModel;
            _suggestionService = suggestionService;
            _bus = bus;
        }

        /// <summary>
        /// Shows the View for viewing a specific video.
        /// </summary>
        [HttpGet]
        public async Task<ViewResult> View(Guid videoId)
        {
            // Get the views for the video and try to find the video by id (in parallel)
            Task<PlayStats> videoViewsTask = _statsReadModel.GetNumberOfPlays(videoId);
            Task<VideoDetails> videoDetailsTask = _videoReadModel.GetVideo(videoId);

            await Task.WhenAll(videoViewsTask, videoDetailsTask);

            VideoDetails videoDetails = videoDetailsTask.Result;
            UserProfileViewModel profile;
            if (videoDetails != null)
            {
                // TODO: Better way than client-side JOIN?
                profile = await GetUserProfile(videoDetails.UserId);

                // Found the video, display it
                return View(new ViewVideoViewModel
                {
                    VideoId = videoId,
                    Title = videoDetails.Name,
                    Description = videoDetails.Description,
                    LocationType = videoDetails.LocationType,
                    Location = videoDetails.Location,
                    Tags = videoDetails.Tags,
                    UploadDate = videoDetails.AddedDate,
                    InProgress = false,
                    Author = profile,
                    Views = videoViewsTask.Result.Views
                });
            }

            // The video might currently be processing (i.e. if it was just uploaded), so try and find it
            UploadedVideo uploadDetails = await _uploadReadModel.GetByVideoId(videoId);
            if (uploadDetails == null)
                throw new ArgumentException(string.Format("Could not find a video with id {0}", videoId));

            // TODO: Better way than client-side JOIN?
            profile = await GetUserProfile(uploadDetails.UserId);

            return View(new ViewVideoViewModel
            {
                VideoId = videoId,
                Title = uploadDetails.Name,
                Description = uploadDetails.Description,
                UploadDate = uploadDetails.AddedDate,
                LocationType = VideoLocationType.Upload,
                Tags = uploadDetails.Tags,
                InProgress = true,
                InProgressJobId = uploadDetails.JobId,
                Author = profile,
                Views = 0
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
        /// Gets related videos for the video specified in the model.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Related(GetRelatedVideosViewModel model)
        {
            RelatedVideos relatedVideos = await _suggestionService.GetRelatedVideos(model.VideoId);

            // TODO:  Better solution than client-side JOINs
            var authorIds = new HashSet<Guid>(relatedVideos.Videos.Select(v => v.UserId));
            Task<IEnumerable<UserProfile>> authorsTask = _userReadModel.GetUserProfiles(authorIds);

            var videoIds = new HashSet<Guid>(relatedVideos.Videos.Select(v => v.VideoId));
            Task<IEnumerable<PlayStats>> statsTask = _statsReadModel.GetNumberOfPlays(videoIds);

            await Task.WhenAll(authorsTask, statsTask);

            return JsonSuccess(new RelatedVideosViewModel
            {
                Videos = relatedVideos.Videos
                                     .Join(authorsTask.Result, vp => vp.UserId, a => a.UserId,
                                           (vp, a) => new { VideoPreview = vp, Author = a })
                                     .Join(statsTask.Result, vpa => vpa.VideoPreview.VideoId, s => s.VideoId,
                                           (vpa, s) => VideoPreviewViewModel.FromDataModel(vpa.VideoPreview, vpa.Author, s, Url))
                                     .ToList()
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

            // TODO:  Better solution than client-side JOINs
            Task<UserProfile> authorTask = _userReadModel.GetUserProfile(userId.Value);
            
            var videoIds = new HashSet<Guid>(userVideos.Videos.Select(v => v.VideoId));
            Task<IEnumerable<PlayStats>> statsTask = _statsReadModel.GetNumberOfPlays(videoIds);

            await Task.WhenAll(authorTask, statsTask);

            return JsonSuccess(new UserVideosViewModel
            {
                UserId = userVideos.UserId,
                Videos = userVideos.Videos.Join(statsTask.Result, v => v.VideoId, s => s.VideoId,
                                                (v, s) => VideoPreviewViewModel.FromDataModel(v, authorTask.Result, s, Url))
                                   .ToList()
            });
        }

        /// <summary>
        /// Gets the most recent videos.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Recent(GetRecentVideosViewModel model)
        {
            LatestVideos recentVideos = await _videoReadModel.GetLastestVideos(new GetLatestVideos
            {
                PageSize = model.PageSize,
                FirstVideoOnPageDate = model.FirstVideoOnPage == null ? (DateTimeOffset?) null : model.FirstVideoOnPage.AddedDate,
                FirstVideoOnPageVideoId = model.FirstVideoOnPage == null ? (Guid?) null : model.FirstVideoOnPage.VideoId
            });

            // TODO:  Better solution than client-side JOINs
            var authorIds = new HashSet<Guid>(recentVideos.Videos.Select(v => v.UserId));
            Task<IEnumerable<UserProfile>> authorsTask = _userReadModel.GetUserProfiles(authorIds);

            var videoIds = new HashSet<Guid>(recentVideos.Videos.Select(v => v.VideoId));
            Task<IEnumerable<PlayStats>> statsTask = _statsReadModel.GetNumberOfPlays(videoIds);

            await Task.WhenAll(authorsTask, statsTask);

            return JsonSuccess(new RecentVideosViewModel
            {
                Videos = recentVideos.Videos
                                     .Join(authorsTask.Result, vp => vp.UserId, a => a.UserId,
                                           (vp, a) => new { VideoPreview = vp, Author = a })
                                     .Join(statsTask.Result, vpa => vpa.VideoPreview.VideoId, s => s.VideoId,
                                           (vpa, s) => VideoPreviewViewModel.FromDataModel(vpa.VideoPreview, vpa.Author, s, Url))
                                     .ToList()
            });
        }

        /// <summary>
        /// Get the ratings data for a video.
        /// </summary>
        [HttpGet, NoCache]
        public async Task<JsonNetResult> GetRatings(GetRatingsViewModel model)
        {
            // We definitely want the overall rating info, so start there
            Task<VideoRating> ratingTask = _ratingsReadModel.GetRating(model.VideoId);

            // If a user is logged in, we also want their rating
            Guid? userId = User.GetCurrentUserId();
            Task<UserVideoRating> userRatingTask = null;
            if (userId.HasValue)
                userRatingTask = _ratingsReadModel.GetRatingFromUser(model.VideoId, userId.Value);

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
            await _bus.Send(new RateVideo
            {
                VideoId = model.VideoId, 
                UserId = User.GetCurrentUserId().Value,
                Rating = model.Rating
            });
            return JsonSuccess();
        }

        private async Task<UserProfileViewModel> GetUserProfile(Guid userId)
        {
            UserProfile profile = await _userReadModel.GetUserProfile(userId);
            return UserProfileViewModel.FromDataModel(profile);
        }
	}
}