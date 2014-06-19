using System;
using System.Web.Mvc;
using KillrVideo.Models.Comments;

namespace KillrVideo.Controllers
{
    public class CommentsController : Controller
    {
        /// <summary>
        /// Gets comments for the specified video.
        /// </summary>
        public JsonResult Index(Guid videoId)
        {
            return new JsonResult();
        }

        public JsonResult Add(AddCommentViewModel model)
        {
            return new JsonResult();
        }
    }
}