using System.Web.Mvc;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller handles upload of videos.
    /// </summary>
    public class UploadController : Controller
    {
        // GET: Upload
        public ActionResult Index()
        {
            return View();
        }
    }
}