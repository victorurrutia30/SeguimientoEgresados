using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace SeguimientoEgresados
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        /// <summary>
        /// Resuelve identidad y roles desde el ticket de FormsAuth (UserData: "Rol|UserId")
        /// y realiza una redirección suave a los dashboards solo cuando corresponde.
        /// </summary>
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            // 1) Resolver principal (identidad + rol)
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null || string.IsNullOrEmpty(authCookie.Value))
                return;

            FormsAuthenticationTicket ticket;
            try
            {
                ticket = FormsAuthentication.Decrypt(authCookie.Value);
            }
            catch
            {
                // Cookie/ticket corrupto: no setear principal
                return;
            }

            if (ticket == null || ticket.Expired)
                return;

            // userData = "Rol|UserId"
            var parts = (ticket.UserData ?? string.Empty).Split('|');
            var role = parts.Length > 0 ? parts[0] : null;

            var identity = new FormsIdentity(ticket);
            var roles = !string.IsNullOrWhiteSpace(role) ? new[] { role } : new string[0];
            var principal = new GenericPrincipal(identity, roles);

            HttpContext.Current.User = principal;
            System.Threading.Thread.CurrentPrincipal = principal;

            // 2) Redirección automática por rol (solo si aplica)
            //    - Solo GET (evitar afectar POST/PUT/DELETE)
            //    - No AJAX
            //    - Evitar rutas estáticas o de autenticación
            //    - Solo cuando el usuario está en Home/Index o raíz
            if (!User.Identity.IsAuthenticated) return;
            if (!string.Equals(Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) return;

            // Detectar AJAX (wrapper MVC)
            var isAjax = new HttpRequestWrapper(Request).IsAjaxRequest();
            if (isAjax) return;

            var currentPath = (Request.Url?.AbsolutePath ?? "/").TrimEnd('/').ToLowerInvariant();

            // Excluir rutas para no generar bucles ni afectar archivos estáticos
            // Nota: ajusta según tu estructura real
            if (currentPath.StartsWith("/autenticacion") ||
                currentPath.StartsWith("/dashboards") ||
                currentPath.StartsWith("/content") ||
                currentPath.StartsWith("/scripts") ||
                currentPath.StartsWith("/bundles") ||
                currentPath.StartsWith("/uploads"))
            {
                return;
            }

            // Si hay returnUrl en querystring, respeta ese flujo
            var returnUrl = Request.QueryString["returnUrl"];
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return;

            // Considerar como "inicio" las rutas home y raíz
            bool esRutaInicio =
                string.IsNullOrEmpty(currentPath) ||          // si quedó vacío tras TrimEnd('/')
                currentPath == "/" ||
                currentPath == "/home" ||
                currentPath == "/home/index";

            if (!esRutaInicio) return;

            // 3) Redirige según rol
            string redirectUrl = null;
            if (!string.IsNullOrWhiteSpace(role))
            {
                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    redirectUrl = "~/Dashboards/Admin";
                else if (role.Equals("Egresado", StringComparison.OrdinalIgnoreCase))
                    redirectUrl = "~/Dashboards/Egresado";
                else if (role.Equals("Empresa", StringComparison.OrdinalIgnoreCase))
                    redirectUrl = "~/Dashboards/Empresa";
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                // Redirección sin lanzar ThreadAbortException
                Response.Redirect(redirectUrl, endResponse: false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }
    }
}
