using System.Web.Mvc;
using KillrVideo.ActionFilters;

namespace KillrVideo
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new LogExceptionsFilter());
            filters.Add(new HandleErrorAttribute());
        }
    }
}
