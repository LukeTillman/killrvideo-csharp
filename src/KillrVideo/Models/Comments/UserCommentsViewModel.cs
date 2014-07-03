using System;
using System.Collections.Generic;

namespace KillrVideo.Models.Comments
{
    /// <summary>
    /// Results of getting a page of comments by a user.
    /// </summary>
    [Serializable]
    public class UserCommentsViewModel
    {
        public Guid UserId { get; set; }
        public IEnumerable<UserCommentViewModel> Comments { get; set; } 
    }
}