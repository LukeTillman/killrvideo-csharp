using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.Windsor;
using KillrVideo.Utils;
using Serilog;

namespace KillrVideo
{
    public class MvcApplication : HttpApplication
    {
        private static IWindsorContainer _container;

        protected void Application_Start()
        {
            // Bootstrap serilog logging
            Log.Logger = new LoggerConfiguration().WriteTo.Trace().CreateLogger();

            Log.Information("KillrVideo is starting");
            
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
            Log.Information("KillrVideo is stopping");

            // Properly clean-up the container on app end
            if (_container != null)
                _container.Dispose();
        }

        protected void Application_Error(Object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex == null)
                return;

            Log.Error(ex, "Uncaught exception in KillrVideo.Web");
        }
    }
}
