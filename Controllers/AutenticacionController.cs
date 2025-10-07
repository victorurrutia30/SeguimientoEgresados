using SeguimientoEgresados.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
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
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public ActionResult Index(string email, string password, string returnUrl)
        {
            if (_auth.Login(email, password, out string rol, out int userId))
            {
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,
                    email,
                    DateTime.Now,
                    DateTime.Now.AddDays(1),
                    false, 
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

                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Autenticacion");
        }

        ///EJEMPLOS
        [Authorize(Roles = "Admin")]
        public class AdministradoresController : Controller
        {
            // Solo admins
        }

        [Authorize(Roles = "Egresado,Empresa")]
        public class PortalController : Controller
        {
            // Egresados y empresas
        }
    }
}