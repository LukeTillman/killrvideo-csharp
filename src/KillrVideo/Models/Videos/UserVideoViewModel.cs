using System;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// ViewModel for a single video posted by a user.
    /// </summary>
    [Serializable]
    public class UserVideoViewModel
    {
        public Guid VideoId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset AddedDate { get; set; }
        public string PreviewImageLocation { get; set; }

        public static UserVideoViewModel FromDataModel(VideoPreview preview)
        {
            if (preview == null) return null;

            return new UserVideoViewModel
            {
                VideoId = preview.VideoId,
                Name = preview.Name,
                AddedDate = preview.AddedDate,
                PreviewImageLocation = preview.PreviewImageLocation
            };
        }
    }
}