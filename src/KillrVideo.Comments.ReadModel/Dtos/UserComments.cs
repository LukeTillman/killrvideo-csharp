using System;
using System.Collections.Generic;

namespace KillrVideo.Comments.ReadModel.Dtos
{
    /// <summary>
    /// Represents a page of comments for a user.
    /// </summary>
    [Serializable]
    public class UserComments
    {
        public Guid UserId { get; set; }
        public IEnumerable<UserComment> Comments { get; set; } 
    }
}
