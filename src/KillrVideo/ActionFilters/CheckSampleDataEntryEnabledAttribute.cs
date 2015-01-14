using System.Web;
using System.Web.Mvc;
using KillrVideo.Utils;

namespace KillrVideo.ActionFilters
{
    /// <summary>
    /// Action filter that checks whether sample data entry is allowed and if not, throws a 404 HttpException.
    /// </summary>
    public class CheckSampleDataEntryEnabledAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // If sample data entry is allowed, do nothing
            if (GlobalConfigs.SampleDataEntryEnabled)
                return;

            // Otherwise, simulate a 404
            throw new HttpException(404, "Page not found");
        }
    }
}