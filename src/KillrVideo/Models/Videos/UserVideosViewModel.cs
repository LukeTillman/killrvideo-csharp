using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Videos
{
    /// <summary>
    /// The results of getting a page of videos for a user.
    /// </summary>
    [Serializable]
    public class UserVideosViewModel
    {
        /// <summary>
        /// The user the videos are for.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The page of videos for the user.
        /// </summary>
        public IEnumerable<VideoPreviewViewModel> Videos { get; set; }

        /// <summary>
        /// The page state token for retrieving the next page of records.
        /// </summary>
        public string PagingState { get; set; }
    }
}