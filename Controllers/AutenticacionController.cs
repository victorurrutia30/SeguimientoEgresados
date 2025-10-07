using SeguimientoEgresados.Servicios;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace SeguimientoEgresados.Controllers
{
    public class AutenticacionController : Controller
    {
        private readonly AuthService _auth = new AuthService();

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index(string returnUrl)
        {
            // Oculta navbar si lo estás usando así en el layout
            ViewBag.HideNavbar = true;

            // Si ya está autenticado, mándalo a su destino
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) &&
                    !returnUrl.StartsWith("/Autenticacion", StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Index(string email, string password, string returnUrl)
        {
            // Asegura que vengan los names correctos desde el form
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.HideNavbar = true;
                ViewBag.Error = "Ingresa tu correo y contraseña.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (_auth.Login(email, password, out string rol, out int userId))
            {
                var ticket = new FormsAuthenticationTicket(
                    1,
                    email,
                    DateTime.Now,
                    DateTime.Now.AddDays(1),
                    false, // isPersistent
                    $"{rol}|{userId}",
                    FormsAuthentication.FormsCookiePath
                );

                string encTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket)
                {
                    HttpOnly = true,
                    Expires = ticket.Expiration
                };
                Response.Cookies.Add(cookie);

                // 1) Si el returnUrl es local y no apunta a Autenticacion, úsalo
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl) &&
                    !returnUrl.StartsWith("/Autenticacion", StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(returnUrl);
                }

                // 2) Si no hay returnUrl válido, manda por rol
                switch (rol)
                {
                    case "Admin":
                        // TODO: cuando tengas tu dashboard admin
                        return RedirectToAction("Index", "Home");
                    case "Egresado":
                        // TODO: dashboard de egresado
                        return RedirectToAction("Index", "Home");
                    case "Empresa":
                        // TODO: dashboard de empresa
                        return RedirectToAction("Index", "Home");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.HideNavbar = true;
            ViewBag.Error = "Usuario o contraseña incorrectos";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Autenticacion");
        }

        [Authorize(Roles = "Admin")]
        public class AdministradoresController : Controller { }

        [Authorize(Roles = "Egresado,Empresa")]
        public class PortalController : Controller { }
    }
}
