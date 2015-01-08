using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.Windsor;
using KillrVideo.Utils;
using log4net;
using log4net.Config;
using Serilog;

namespace KillrVideo
{
    public class MvcApplication : HttpApplication
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (MvcApplication));

        private static IWindsorContainer _container;

        protected void Application_Start()
        {
            // Bootstrap log4net logging and Serilog logger
            XmlConfigurator.Configure();
            Log.Logger = new LoggerConfiguration().WriteTo.Log4Net().CreateLogger();
            
            Logger.Info("KillrVideo is starting");

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
            Logger.Info("KillrVideo is stopping");

            // Properly clean-up the container on app end
            if (_container != null)
                _container.Dispose();
        }

        protected void Application_Error(Object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex == null)
                return;

            Logger.Error(string.Empty, ex);
        }
    }
}
