using Microsoft.Owin;
using Owin;
//using Microsoft.Extensions.DependencyInjection;

[assembly: OwinStartupAttribute(typeof(ASTDAT.Web.Startup))]
namespace ASTDAT.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //var services = new ServiceCollection();
            //ConfigureServices(services);
            //Install-Package Microsoft.Extensions.DependencyInjection -Version 2.2.0

            ConfigureAuth(app);
        }
    }
}
