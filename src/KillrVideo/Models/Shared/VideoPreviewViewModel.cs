using System;
using System.Web.Mvc;
using KillrVideo.Data.PlaybackStats.Dtos;
using KillrVideo.Data.Users.Dtos;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Models.Shared
{
    /// <summary>
    /// View model for a video preview.
    /// </summary>
    [Serializable]
    public class VideoPreviewViewModel
    {
        public Guid VideoId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset AddedDate { get; set; }
        public string PreviewImageLocation { get; set; }
        public string AuthorFirstName { get; set; }
        public string AuthorLastName { get; set; }
        public string AuthorProfileUrl { get; set; }
        public long Views { get; set; }

        /// <summary>
        /// A static mapper function for mapping from the data model to this ViewModel object.
        /// </summary>
        public static VideoPreviewViewModel FromDataModel(VideoPreview preview, UserProfile author, PlayStats stats, UrlHelper urlHelper)
        {
            if (preview == null) return null;

            return new VideoPreviewViewModel
            {
                VideoId = preview.VideoId,
                Name = preview.Name,
                AddedDate = preview.AddedDate,
                PreviewImageLocation = preview.PreviewImageLocation,
                AuthorFirstName = author.FirstName,
                AuthorLastName = author.LastName,
                AuthorProfileUrl = urlHelper.Action("Info", "Account", new { userId = author.UserId }),
                Views = stats.Views
            };
        }
    }
}