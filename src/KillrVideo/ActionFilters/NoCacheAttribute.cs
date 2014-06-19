using System.Web;
using System.Web.Mvc;

namespace KillrVideo.ActionFilters
{
    /// <summary>
    /// Sets the cache header for the response to no cache (useful for stopping IE from caching JSON HTTP GETs).
    /// </summary>
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        }
    }
}