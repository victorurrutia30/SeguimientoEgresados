using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SeguimientoEgresados.Controllers
{
    public class RegistroController : Controller
    {        
        private readonly Servicios.Registro _servicioRegistro = new Servicios.Registro();
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult RegistrarEgresado(string numeroDocumento, string nombres, string apellidos,
            string email, string telefono, int carrera, DateTime fechaGraduaacion, decimal promedio, bool consentimiento,
            HttpPostedFileBase CV, int experiencia, string habilidades, string idiomas, string certificaciones,
            string password)
        {
            var resultado = _servicioRegistro.RegistrarEgresado(numeroDocumento, nombres, apellidos,
                email, telefono, carrera, fechaGraduaacion, promedio, consentimiento,
                CV, experiencia, habilidades, idiomas, certificaciones, password);

            if (resultado.Exito)
            {
                return Json(new { success = true, message = resultado.Mensaje },JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false, message = resultado.Mensaje },JsonRequestBehavior.AllowGet);
        }
        // POST: Guardar encuesta (trabaja o no)
        [HttpPost]
        public JsonResult GuardarSituacionLaboral(
            int idEgresado,
            bool trabajandoActualmente,
            string empresaActual,
            string cargoActual,
            string rangoSalarial,
            string modalidadTrabajo,
            int? satisfaccionTrabajo,
            bool? usaConocimientosCarrera,
            int? tiempoConseguirTrabajo,
            HttpPostedFileBase CV
        )
        {
            var resultado = _servicioRegistro.GuardarSituacionLaboral(
                idEgresado,
                trabajandoActualmente,
                empresaActual,
                cargoActual,
                rangoSalarial,
                modalidadTrabajo,
                satisfaccionTrabajo,
                usaConocimientosCarrera,
                tiempoConseguirTrabajo,
                CV
            );

            if (resultado.Exito)
                return Json(new { success = true, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);

            return Json(new { success = false, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);
        }

    }
}