using System.Web.Mvc;
using System.Web.Routing;

namespace KillrVideo
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Default route for viewing videos
            routes.MapRoute(
                name: "ViewVideo",
                url: "view/{videoId}",
                defaults: new { controller = "Videos", action = "View" }
            );

            // Default catch-all route
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
