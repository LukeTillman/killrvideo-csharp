using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.Windsor;

namespace KillrVideo
{
    public class MvcApplication : HttpApplication
    {
        private static IWindsorContainer _container;

        protected void Application_Start()
        {
            // Bootstrap Windsor container and tell MVC to use Windsor to create IController instances
            _container = WindsorConfig.CreateContainer();
            ControllerBuilder.Current.SetControllerFactory(new WindsorControllerFactory(_container.Kernel));

            // Do MVC-related bootstrapping
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_End()
        {
            // Properly clean-up the container on app end
            if (_container != null)
                _container.Dispose();
        }
    }
}
