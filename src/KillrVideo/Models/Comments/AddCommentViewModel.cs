using System;

namespace KillrVideo.Models.Comments
{
    public class AddCommentViewModel
    {
        public Guid VideoId { get; set; }
        public string Comment { get; set; }
    }
}