using System.Web.Mvc;
using ASTDAT.Web.Infrastructure;

namespace ASTDAT.Web.Controllers
{
    [Authorize]
    public class CarrierController : Controller
    {
        public ActionResult Index()
        {
            if (!LoadboardPermissions.ShouldUseCarrierPortal(User))
                return RedirectToAction("Index", "Home");

            if (!LoadboardPermissions.CanViewLoads(User))
                return View("Pending");

            return View();
        }
    }
}
