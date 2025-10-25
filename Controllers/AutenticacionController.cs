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

        // GET: /Autenticacion
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index(string returnUrl)
        {
            // Oculta navbar en la pantalla de login
            ViewBag.HideNavbar = true;

            // Si ya está autenticado, envía a returnUrl (si es local) o Home
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl) &&
                    !returnUrl.StartsWith("/Autenticacion", StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Autenticacion
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string email, string password, string returnUrl, bool rememberMe = false)
        {
            ViewBag.HideNavbar = true;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Los datos no pueden ir vacíos.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (_auth.Login(email, password, out string rol, out int userId))
            {
                // Ticket con rol|userId en UserData (luego se parsea en Global.asax para Roles)
                var issueDate = DateTime.Now;
                var expiration = rememberMe ? issueDate.AddDays(7) : issueDate.AddHours(12);

                var ticket = new FormsAuthenticationTicket(
                    version: 1,
                    name: email,
                    issueDate: issueDate,
                    expiration: expiration,
                    isPersistent: rememberMe,
                    userData: $"{rol}|{userId}",
                    cookiePath: FormsAuthentication.FormsCookiePath
                );

                string encTicket = FormsAuthentication.Encrypt(ticket);

                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket)
                {
                    HttpOnly = true,
                    Expires = rememberMe ? expiration : DateTime.MinValue // si no es persistente, que sea cookie de sesión
                };

                // Endurecer cookie
                cookie.Secure = Request.IsSecureConnection; // requiere HTTPS para marcar Secure
#if NET472 || NET48
                cookie.SameSite = SameSiteMode.Lax;
#endif

                Response.Cookies.Add(cookie);

                // Guarda algo en sesión si te resulta útil
                Session["UserId"] = userId;
                Session["UserRole"] = rol;

                // 1) Usa returnUrl si es local y no apunta a Autenticacion
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl) &&
                    !returnUrl.StartsWith("/Autenticacion", StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(returnUrl);
                }

                // 2) Sin returnUrl, redirige por rol
                switch (rol)
                {
                    case "Admin":
                        return RedirectToAction("Admin", "Dashboards");
                    case "Egresado":
                        return RedirectToAction("Egresado", "Dashboards");
                    case "Empresa":
                        return RedirectToAction("Empresa", "Dashboards");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }

            // Credenciales inválidas
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Autenticacion/Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            try
            {
                FormsAuthentication.SignOut();
                Session.Clear();
                Session.Abandon();

                // Invalida la cookie en el cliente por si acaso
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, "")
                {
                    Expires = DateTime.Now.AddDays(-1),
                    HttpOnly = true
                };
                Response.Cookies.Add(cookie);
            }
            catch { /* swallow: no exponemos detalles */ }

            // Redirige a Home/Index como pediste
            return RedirectToAction("Index", "Home");
        }
    }
}
