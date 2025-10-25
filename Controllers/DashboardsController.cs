using System;
using System.Web.Mvc;
using SeguimientoEgresados.Servicios;

namespace SeguimientoEgresados.Controllers
{
    [Authorize(Roles = "Egresado")]
    public class DashboardController : Controller
    {
        private readonly DashboardService _svc;

        public DashboardController()
        {
            _svc = new DashboardService();
        }

        // Para pruebas unitarias / inyección
        public DashboardController(DashboardService service)
        {
            _svc = service ?? new DashboardService();
        }

        // =============================================
        // VISTA PRINCIPAL (renderiza el Razor con el VM)
        // =============================================
        [HttpGet]
        [Route("Dashboards/Egresado")] // <-- agrega esta ruta para que /Dashboards/Egresado funcione
        public ActionResult Index()
        {
            var id = CurrentEgresadoId();
            if (id == null) return RedirectToAction("Index", "Autenticacion");

            var vm = _svc.BuildDashboard(id.Value);
            // Renderiza tu vista existente en Views/Dashboards/Egresado.cshtml
            return View("~/Views/Dashboards/Egresado.cshtml", vm);
        }

        // =============================================
        // ENDPOINTS JSON PARA WIDGETS (AJAX FRIENDLY)
        // =============================================
        [HttpGet]
        public ActionResult Resumen()
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetResumen(id.Value);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult CvCard()
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetCvCard(id.Value);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult MatchingTop(int top = 5)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetMatchingTop(id.Value, top);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Procesos(int take = 8)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetProcesos(id.Value, take);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Notificaciones(int take = 6)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetNotificaciones(id.Value, take);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Encuesta()
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetEncuestaEstado(id.Value);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Evaluaciones(int take = 5)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetEvaluacionesRecientes(id.Value, take);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult EmpresasQueVieron(int take = 5)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false }, JsonRequestBehavior.AllowGet);

            var data = _svc.GetEmpresasQueVieron(id.Value, take);
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        // =========================
        // ACCIONES DE USUARIO
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleDisponibleBusqueda(bool disponible)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false });

            var ok = _svc.SetDisponibleBusqueda(id.Value, disponible);
            return Json(new { ok });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarPrivacidad(string nivel)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false });

            var ok = _svc.SetPrivacidadCv(id.Value, nivel);
            return Json(new { ok });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarNotificacionLeida(int idNotificacion)
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false });

            var ok = _svc.MarkNotificationRead(id.Value, idNotificacion);
            return Json(new { ok });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarTodasLeidas()
        {
            var id = CurrentEgresadoId();
            if (id == null) return Json(new { ok = false });

            var count = _svc.MarkAllNotificationsRead(id.Value);
            return Json(new { ok = true, updated = count });
        }

        // =========================
        // Helpers
        // =========================
        private int? CurrentEgresadoId()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var email = User.Identity.Name;
            return _svc.GetEgresadoIdByEmail(email);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _svc?.Dispose();
            base.Dispose(disposing);
        }
    }
}
