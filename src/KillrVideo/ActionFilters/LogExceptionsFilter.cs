using System.Web.Mvc;
using Serilog;

namespace KillrVideo.ActionFilters
{
    /// <summary>
    /// Filter for logging exceptions in MVC controllers since if we use the HandleErrorAttribute, it won't fire the
    /// Application_Error event for errors.
    /// </summary>
    public class LogExceptionsFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            Log.Error(filterContext.Exception, "Uncaught exception in KillrVideo");
        }
    }
}