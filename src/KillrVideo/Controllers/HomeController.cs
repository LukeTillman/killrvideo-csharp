using System.Web.Mvc;

namespace KillrVideo.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Shows the home page.
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }
    }
}