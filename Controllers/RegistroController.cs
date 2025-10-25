using SeguimientoEgresados.Models; // Contexto EF (SistemaEgresadosUtecEntities)
using System;
using System.Web;
using System.Web.Mvc;

namespace SeguimientoEgresados.Controllers
{
    public class RegistroController : Controller
    {
        private readonly Servicios.Registro _servicioRegistro = new Servicios.Registro();
        private readonly Servicios.Utilidades _servicioUtilidades = new Servicios.Utilidades();
        private readonly SistemaEgresadosUtecEntities db = new SistemaEgresadosUtecEntities();

        [AllowAnonymous]
        public ActionResult Index()
        {
            // Cargar Carreras activas para el combo
            ViewBag.Carreras = _servicioUtilidades.ObtenerCarreras().Datos;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public JsonResult RegistrarEgresado(
    string numeroDocumento, string nombres, string apellidos,
    string email, string telefono, int carrera, DateTime fechaGraduacion, decimal promedio, bool consentimiento,
    HttpPostedFileBase CV, int experiencia, string habilidades, string idiomas, string certificaciones,
    string password)
        {
            // Hash del password antes de guardar
            var auth = new Servicios.AuthService();
            var hash = auth.HashPassword(password);

            var resultado = _servicioRegistro.RegistrarEgresado(
    numeroDocumento, nombres, apellidos,
    email, telefono, carrera, fechaGraduacion, promedio, consentimiento,
    CV, experiencia, habilidades, idiomas, certificaciones, hash);

            if (resultado.Exito)
            {
                return Json(new
                {
                    success = true,
                    message = resultado.Mensaje,
                    idEgresado = (resultado.Datos as dynamic)?.id
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public JsonResult GuardarSituacionLaboral(
            int idEgresado,
            bool trabajandoActualmente,
            string empresaActual,
            string cargoActual,
            string rangoSalarial,
            string modalidadTrabajo,
            byte? satisfaccionTrabajo,
            bool? usaConocimientosCarrera,
            int? tiempoConseguirTrabajo,
            // nuevos
            string contactaUniversidad,
            byte? deseaContacto,
            byte? dispuestoEncuestaSemestral,
            string metodoInicioSesion,
            string respuestasJson,
            string sugerenciaFuncionalidad
        )
        {
            var resultado = _servicioRegistro.GuardarSituacionLaboral(
                idEgresado, trabajandoActualmente,
                empresaActual, cargoActual, rangoSalarial, modalidadTrabajo,
                satisfaccionTrabajo, usaConocimientosCarrera, tiempoConseguirTrabajo,
                null, // CV en este paso lo dejamos null
                contactaUniversidad, deseaContacto, dispuestoEncuestaSemestral,
                metodoInicioSesion, respuestasJson, sugerenciaFuncionalidad
            );

            if (resultado.Exito)
                return Json(new { success = true, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);

            return Json(new { success = false, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
