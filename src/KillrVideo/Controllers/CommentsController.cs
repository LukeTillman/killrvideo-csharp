using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Comments;
using KillrVideo.Comments.Dtos;
using KillrVideo.Models.Comments;
using KillrVideo.UserManagement;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.Utils;
using KillrVideo.VideoCatalog;
using KillrVideo.VideoCatalog.Dtos;

namespace KillrVideo.Controllers
{
    public class CommentsController : ConventionControllerBase
    {
        private readonly ICommentsService _comments;
        private readonly IVideoCatalogService _videoCatalog;
        private readonly IUserManagementService _userManagement;
        
        public CommentsController(ICommentsService comments, IVideoCatalogService videoCatalog, IUserManagementService userManagement)
        {
            if (comments == null) throw new ArgumentNullException("comments");
            if (videoCatalog == null) throw new ArgumentNullException("videoCatalog");
            if (userManagement == null) throw new ArgumentNullException("userManagement");
            _comments = comments;
            _videoCatalog = videoCatalog;
            _userManagement = userManagement;
        }

        /// <summary>
        /// Gets a page of the comments for a video.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> ByVideo(GetVideoCommentsViewModel model)
        {
            VideoComments result = await _comments.GetVideoComments(new GetVideoComments
            {
                VideoId = model.VideoId,
                PageSize = model.PageSize,
                FirstCommentIdOnPage = model.FirstCommentIdOnPage
            });

            // For the ViewModel, we also want to include the information about a user who made the comments on the video, so
            // get the user profile information for the comments and then use a LINQ to Objects Join to merge the two together
            // (this should be OK since the dataset should be small)
            IEnumerable<UserProfile> userProfiles = await _userManagement.GetUserProfiles(result.Comments.Select(c => c.UserId).ToHashSet());

            var returnModel = new VideoCommentsViewModel
            {
                VideoId = result.VideoId,
                Comments = result.Comments.Join(userProfiles, c => c.UserId, up => up.UserId, (c, up) => new VideoCommentViewModel
                {
                    CommentId = c.CommentId,
                    Comment = c.Comment,
                    CommentTimestamp = c.CommentTimestamp,
                    UserProfileUrl = Url.Action("Info", "Account", new { userId = c.UserId }),
                    UserFirstName = up.FirstName,
                    UserLastName = up.LastName,
                    UserGravatarImageUrl = GravatarHasher.GetImageUrlForEmailAddress(up.EmailAddress)
                }).ToList()
            };

            return JsonSuccess(returnModel);
        }

        /// <summary>
        /// Gets a page of the comments made by a user.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> ByUser(GetUserCommentsViewModel model)
        {
            // If no user was specified, default to the current logged in user
            Guid? userId = model.UserId ?? User.GetCurrentUserId();
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "No user specified and no user currently logged in.");
                return JsonFailure();
            }

            // Get a page of comments for the user, then look up video details for those videos
            UserComments result = await _comments.GetUserComments(new GetUserComments
            {
                UserId = userId.Value,
                PageSize = model.PageSize,
                FirstCommentIdOnPage = model.FirstCommentIdOnPage
            });

            // For the ViewModel, we want to add information about the video to each comment as well, so get the video preview 
            // information for the comments and then use a LINQ to objects Join to merge the two together (this should be OK since
            // the dataset should be small since we're doing a page at a time)
            IEnumerable<VideoPreview> videoPreviews = await _videoCatalog.GetVideoPreviews(result.Comments.Select(c => c.VideoId).ToHashSet());
            
            var returnModel = new UserCommentsViewModel
            {
                UserId = result.UserId,
                Comments = result.Comments.Join(videoPreviews, c => c.VideoId, vp => vp.VideoId, (c, vp) => new UserCommentViewModel
                {
                    CommentId = c.CommentId,
                    Comment = c.Comment,
                    CommentTimestamp = c.CommentTimestamp,
                    VideoViewUrl = Url.Action("View", "Videos", new { videoId = c.VideoId }),
                    VideoName = vp.Name,
                    VideoPreviewImageLocation = vp.PreviewImageLocation
                }).ToList()
            };

            return JsonSuccess(returnModel);
        }

        /// <summary>
        /// Adds a comment to a video.
        /// </summary>
        [HttpPost, Authorize]
        public async Task<JsonNetResult> Add(AddCommentViewModel model)
        {
            // Shouldn't throw because of the Authorize attribute
            Guid userId = User.GetCurrentUserId().Value;

            var commentTimestamp = DateTimeOffset.UtcNow;

            // Add the new comment
            var commentOnVideo = new CommentOnVideo
            {
                UserId = userId,
                VideoId = model.VideoId,
                CommentId = GuidGenerator.GenerateTimeBasedGuid(commentTimestamp),
                Comment = model.Comment
            };

            await _comments.CommentOnVideo(commentOnVideo);

            // Lookup the current user's information to include in the return data
            UserProfile userInfo = await _userManagement.GetUserProfile(userId);

            return JsonSuccess(new VideoCommentViewModel
            {
                CommentId = commentOnVideo.CommentId,
                Comment = commentOnVideo.Comment,
                CommentTimestamp = commentTimestamp,
                UserProfileUrl = Url.Action("Info", "Account", new { userId }),
                UserFirstName = userInfo.FirstName,
                UserLastName = userInfo.LastName,
                UserGravatarImageUrl = GravatarHasher.GetImageUrlForEmailAddress(userInfo.EmailAddress)
            });
        }
    }
}