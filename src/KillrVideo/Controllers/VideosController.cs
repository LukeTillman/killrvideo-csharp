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
using KillrVideo.Statistics;
using KillrVideo.Statistics.Dtos;
using KillrVideo.SuggestedVideos;
using KillrVideo.SuggestedVideos.Dtos;
using KillrVideo.UserManagement;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.VideoCatalog;
using KillrVideo.VideoCatalog.Dtos;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller for viewing videos.
    /// </summary>
    public class VideosController : ConventionControllerBase
    {
        private readonly IVideoCatalogService _videoCatalog;
        private readonly IUserManagementService _userManagement;
        private readonly IStatisticsService _stats;
        private readonly IRatingsService _ratings;
        private readonly ISuggestVideos _suggestions;

        public VideosController(IVideoCatalogService videoCatalog, IUserManagementService userManagement, IStatisticsService stats,
                                IRatingsService ratings, ISuggestVideos suggestions)
        {
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            if (userManagement == null) throw new ArgumentNullException("userManagement");
            if (stats == null) throw new ArgumentNullException("stats");
            if (ratings == null) throw new ArgumentNullException("ratings");
            if (suggestions == null) throw new ArgumentNullException("suggestions");
            _videoCatalog = videoCatalog;
            _userManagement = userManagement;
            _stats = stats;
            _ratings = ratings;
            _suggestions = suggestions;
        }

        /// <summary>
        /// Shows the View for viewing a specific video.
        /// </summary>
        [HttpGet]
        public async Task<ViewResult> View(Guid videoId)
        {
            // Get the views for the video and try to find the video by id (in parallel)
            Task<PlayStats> videoViewsTask = _stats.GetNumberOfPlays(videoId);
            Task<VideoDetails> videoDetailsTask = _videoCatalog.GetVideo(videoId);

            await Task.WhenAll(videoViewsTask, videoDetailsTask);

            VideoDetails videoDetails = videoDetailsTask.Result;
            if (videoDetails == null)
                throw new ArgumentException(string.Format("Could not find a video with id {0}", videoId));

            // TODO: Better way than client-side JOIN?
            UserProfileViewModel profile = await GetUserProfile(videoDetails.UserId);

            bool inProgress = videoDetails.LocationType == VideoLocationType.Upload && videoDetails.Location == null;

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
                InProgress = inProgress,
                Author = profile,
                Views = videoViewsTask.Result.Views
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
            RelatedVideos relatedVideos = await _suggestions.GetRelatedVideos(new RelatedVideosQuery
            {
                VideoId = model.VideoId,
                PagingState = model.PagingState,
                PageSize = model.PageSize
            });

            // TODO:  Better solution than client-side JOINs
            var authorIds = new HashSet<Guid>(relatedVideos.Videos.Select(v => v.UserId));
            Task<IEnumerable<UserProfile>> authorsTask = _userManagement.GetUserProfiles(authorIds);

            var videoIds = new HashSet<Guid>(relatedVideos.Videos.Select(v => v.VideoId));
            Task<IEnumerable<PlayStats>> statsTask = _stats.GetNumberOfPlays(videoIds);

            await Task.WhenAll(authorsTask, statsTask);

            return JsonSuccess(new RelatedVideosViewModel
            {
                VideoId = relatedVideos.VideoId,
                Videos = relatedVideos.Videos
                                      .Join(authorsTask.Result, vp => vp.UserId, a => a.UserId,
                                            (vp, a) => new { VideoPreview = vp, Author = a })
                                      .Join(statsTask.Result, vpa => vpa.VideoPreview.VideoId, s => s.VideoId,
                                            (vpa, s) => VideoPreviewViewModel.FromDataModel(vpa.VideoPreview, vpa.Author, s, Url))
                                      .ToList(),
                PagingState = relatedVideos.PagingState
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

            UserVideos userVideos = await _videoCatalog.GetUserVideos(new GetUserVideos
            {
                UserId = userId.Value,
                PageSize = model.PageSize,
                PagingState = model.PagingState
            });

            // TODO:  Better solution than client-side JOINs
            Task<UserProfile> authorTask = _userManagement.GetUserProfile(userId.Value);
            
            var videoIds = new HashSet<Guid>(userVideos.Videos.Select(v => v.VideoId));
            Task<IEnumerable<PlayStats>> statsTask = _stats.GetNumberOfPlays(videoIds);

            await Task.WhenAll(authorTask, statsTask);

            return JsonSuccess(new UserVideosViewModel
            {
                UserId = userVideos.UserId,
                Videos = userVideos.Videos.Join(statsTask.Result, v => v.VideoId, s => s.VideoId,
                                                (v, s) => VideoPreviewViewModel.FromDataModel(v, authorTask.Result, s, Url))
                                   .ToList(),
                PagingState = userVideos.PagingState
            });
        }

        /// <summary>
        /// Gets the most recent videos.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Recent(GetRecentVideosViewModel model)
        {
            LatestVideos recentVideos = await _videoCatalog.GetLastestVideos(new GetLatestVideos
            {
                PageSize = model.PageSize,
                PagingState = model.PagingState
            });

            // TODO:  Better solution than client-side JOINs
            var authorIds = new HashSet<Guid>(recentVideos.Videos.Select(v => v.UserId));
            Task<IEnumerable<UserProfile>> authorsTask = _userManagement.GetUserProfiles(authorIds);

            var videoIds = new HashSet<Guid>(recentVideos.Videos.Select(v => v.VideoId));
            Task<IEnumerable<PlayStats>> statsTask = _stats.GetNumberOfPlays(videoIds);

            await Task.WhenAll(authorsTask, statsTask);

            return JsonSuccess(new RecentVideosViewModel
            {
                Videos = recentVideos.Videos
                                     .Join(authorsTask.Result, vp => vp.UserId, a => a.UserId,
                                           (vp, a) => new { VideoPreview = vp, Author = a })
                                     .Join(statsTask.Result, vpa => vpa.VideoPreview.VideoId, s => s.VideoId,
                                           (vpa, s) => VideoPreviewViewModel.FromDataModel(vpa.VideoPreview, vpa.Author, s, Url))
                                     .ToList(),
                PagingState = recentVideos.PagingState
            });
        }

        /// <summary>
        /// Gets videos recommended for the currently logged in user.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Recommended(GetRecommendedVideosViewModel model)
        {
            // If no user was specified, default to the current logged in user
            Guid? userId = User.GetCurrentUserId();
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "No user currently logged in.");
                return JsonFailure();
            }

            SuggestedVideos.Dtos.SuggestedVideos suggestions = await _suggestions.GetSuggestions(new SuggestedVideosQuery
            {
                UserId = userId.Value,
                PagingState = model.PagingState,
                PageSize = model.PageSize
            });

            // TODO:  Better solution than client-side JOINs
            var authorIds = new HashSet<Guid>(suggestions.Videos.Select(v => v.UserId));
            Task<IEnumerable<UserProfile>> authorsTask = _userManagement.GetUserProfiles(authorIds);

            var videoIds = new HashSet<Guid>(suggestions.Videos.Select(v => v.VideoId));
            Task<IEnumerable<PlayStats>> statsTask = _stats.GetNumberOfPlays(videoIds);

            await Task.WhenAll(authorsTask, statsTask);

            return JsonSuccess(new RecommendedVideosViewModel
            {
                Videos = suggestions.Videos
                                    .Join(authorsTask.Result, vp => vp.UserId, a => a.UserId,
                                          (vp, a) => new { VideoPreview = vp, Author = a })
                                    .Join(statsTask.Result, vpa => vpa.VideoPreview.VideoId, s => s.VideoId,
                                          (vpa, s) => VideoPreviewViewModel.FromDataModel(vpa.VideoPreview, vpa.Author, s, Url))
                                    .ToList(),
                PagingState = suggestions.PagingState
            });
        }

        /// <summary>
        /// Get the ratings data for a video.
        /// </summary>
        [HttpGet, NoCache]
        public async Task<JsonNetResult> GetRatings(GetRatingsViewModel model)
        {
            // We definitely want the overall rating info, so start there
            Task<VideoRating> ratingTask = _ratings.GetRating(model.VideoId);

            // If a user is logged in, we also want their rating
            Guid? userId = User.GetCurrentUserId();
            Task<UserVideoRating> userRatingTask = null;
            if (userId.HasValue)
                userRatingTask = _ratings.GetRatingFromUser(model.VideoId, userId.Value);

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
            await _ratings.RateVideo(new RateVideo
            {
                VideoId = model.VideoId, 
                UserId = User.GetCurrentUserId().Value,
                Rating = model.Rating
            });
            return JsonSuccess();
        }

        private async Task<UserProfileViewModel> GetUserProfile(Guid userId)
        {
            UserProfile profile = await _userManagement.GetUserProfile(userId);
            return UserProfileViewModel.FromDataModel(profile);
        }
	}
}