using ASTDAT.Data.Models;
using ASTDAT.Tools;
using ASTDAT.Web.Infrastructure;
using ASTDAT.Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace ASTDAT.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static DateTime ApplicationStarted;

        protected void Application_Start()
        {
            ApplicationStarted = DateTime.Now;

            Logger.Enabled = System.Configuration.ConfigurationManager.AppSettings["LoggerEnabled"] == "true";
            Logger.Folder = Server.MapPath("~/App_data");
            Logger.Write($"Application started {ApplicationStarted}");


            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            Database.SetInitializer<ApplicationDbContext>(null);
            Database.SetInitializer<DBContext>(null);

            IntegrationService.Instance.DeleteOlder30();
        }

        protected void Application_End()
        {
            Logger.Write($"Application stoped {DateTime.Now}");
        }
    }
}
