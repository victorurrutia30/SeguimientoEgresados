using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace SeguimientoEgresados.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        [Authorize(Roles = "Admin")]
        public ActionResult Admin()
        {
            ViewBag.Role = "Admin";
            return View();
        }

        [Authorize(Roles = "Egresado")]
        public ActionResult Egresado()
        {
            ViewBag.Role = "Egresado";
            return View();
        }

        [Authorize(Roles = "Empresa")]
        public ActionResult Empresa()
        {
            ViewBag.Role = "Empresa";
            return View();
        }
    }
}
