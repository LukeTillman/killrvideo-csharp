using System;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.Models.Videos
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

        /// <summary>
        /// A static mapper function for mapping from the data model to this ViewModel object.
        /// </summary>
        public static VideoPreviewViewModel FromDataModel(VideoPreview preview)
        {
            if (preview == null) return null;

            return new VideoPreviewViewModel
            {
                VideoId = preview.VideoId,
                Name = preview.Name,
                AddedDate = preview.AddedDate,
                PreviewImageLocation = preview.PreviewImageLocation
            };
        }
    }
}