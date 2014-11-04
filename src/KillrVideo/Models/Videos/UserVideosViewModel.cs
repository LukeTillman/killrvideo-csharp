using System;
using System.Collections.Generic;
using KillrVideo.Models.Shared;

namespace KillrVideo.Models.Videos
{
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
        public IEnumerable<UserVideoViewModel> Videos { get; set; }
    }
}