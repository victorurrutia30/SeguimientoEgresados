using System;
using System.Collections.Generic;
using System.Linq;
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
        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        {
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                try
                {
                    var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                    string[] data = ticket.UserData.Split('|');
                    string rol = data[0];
                    string id = data[1];

                    var identity = new System.Security.Principal.GenericIdentity(ticket.Name);
                    var principal = new System.Security.Principal.GenericPrincipal(identity, new[] { rol });
                    HttpContext.Current.User = principal;
                }
                catch
                {
                    FormsAuthentication.SignOut();
                }
            }
        }

    }
}
