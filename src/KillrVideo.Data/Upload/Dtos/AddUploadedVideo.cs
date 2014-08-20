using System;
using System.Collections.Generic;

namespace KillrVideo.Data.Upload.Dtos
{
    /// <summary>
    /// DTO for adding a new uploaded video.
    /// </summary>
    [Serializable]
    public class AddUploadedVideo
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ISet<string> Tags { get; set; }

        /// <summary>
        /// The encoding job Id from Azure Media Services.
        /// </summary>
        public string JobId { get; set; }

        public AddUploadedVideo()
        {
            Tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
