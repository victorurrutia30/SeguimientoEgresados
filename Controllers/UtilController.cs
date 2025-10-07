using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace SeguimientoEgresados.Controllers
{
    public class UtilController : Controller
    {
        [AllowAnonymous]
        public ContentResult Hash(string pwd = "admin123!")
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(pwd);
            return Content(hash, "text/plain");
        }
    }
}
