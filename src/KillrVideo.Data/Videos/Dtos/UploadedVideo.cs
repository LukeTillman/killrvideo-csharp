using System;
using System.Collections.Generic;

namespace KillrVideo.Data.Videos.Dtos
{
    /// <summary>
    /// DTO that represents an uploaded video.
    /// </summary>
    [Serializable]
    public class UploadedVideo
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ISet<string> Tags { get; set; }
        public DateTimeOffset AddedDate { get; set; }

        /// <summary>
        /// The encoding job Id from Azure Media Services.
        /// </summary>
        public string JobId { get; set; }

        public UploadedVideo()
        {
            Tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
