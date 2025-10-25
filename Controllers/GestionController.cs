using SeguimientoEgresados.Models;
using SeguimientoEgresados.Servicios;
using System;
using System.Web.Mvc;

namespace SeguimientoEgresados.Controllers
{
    // [Authorize(Roles = "Administrador")] // <-- Solo administradores pueden usar este controlador
    public class GestionController : Controller
    {
        private readonly Gestion _servicioGestion = new Servicios.Gestion();
        private readonly AuthService _authService;
        private readonly SistemaEgresadosUtecEntities db = new SistemaEgresadosUtecEntities();


        // ===============================
        // EMPRESAS
        // ===============================
        [HttpGet]
        public JsonResult ObtenerEmpresas()
        {
            var empresas = _servicioGestion.ObtenerEmpresas();
            return Json(empresas, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CrearEmpresa(
             string razonSocial,
             string nit,
             string emailContacto,
             string telefono,
             string direccion,
             string sectorEconomico,
             string tamañoEmpresa,
             bool vinculadaUniversidad = true,
             DateTime? fechaRegistro = null,
             bool estadoActivo = true,
             decimal puntuacionEmpresa = 0.0m,
             int totalContrataciones = 0)
        {
            var resultado = _servicioGestion.CrearEmpresa(
                razonSocial, nit, emailContacto, telefono, direccion, sectorEconomico, tamañoEmpresa,
                vinculadaUniversidad, fechaRegistro, estadoActivo, puntuacionEmpresa, totalContrataciones);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje,
                data = resultado.Datos
            });
        }

        [HttpPost]
        public JsonResult ActualizarEmpresa(
            int id,
            string razonSocial,
            string nit,
            string emailContacto,
            string telefono,
            string direccion,
            string sectorEconomico,
            string tamañoEmpresa,
            bool vinculadaUniversidad,
            DateTime? fechaRegistro = null,
            bool estadoActivo = true,
            decimal puntuacionEmpresa = 5.0m,
            int totalContrataciones = 0)
        {
            var resultado = _servicioGestion.ActualizarEmpresa(
                id, razonSocial, nit, emailContacto, telefono, direccion, sectorEconomico, tamañoEmpresa,
                vinculadaUniversidad, fechaRegistro, estadoActivo, puntuacionEmpresa, totalContrataciones);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }


        [HttpPost]
        public JsonResult EliminarEmpresa(int id)
        {
            var resultado = _servicioGestion.EliminarEmpresa(id);
            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        // ===============================
        // USUARIOS DE EMPRESA
        // ===============================
        [HttpGet]
        public JsonResult ObtenerUsuariosEmpresa(int? empresaId = null)
        {
            var usuarios = _servicioGestion.ObtenerUsuariosEmpresa(empresaId);
            return Json(usuarios, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CrearUsuarioEmpresa(string nombreUsuario, string email, int empresaId, string passwordHash,
      string nombreCompleto, string cargo = null)
        {

            // Hash del password antes de guardar
            var auth = new Servicios.AuthService();
            var hash = auth.HashPassword(passwordHash);

            // Llama al servicio con el hash generado
            var resultado = _servicioGestion.CrearUsuarioEmpresa(
                empresaId, nombreUsuario, email, hash, nombreCompleto, cargo);

            if (resultado.Exito)
            {
                return Json(new
                {
                    success = true,
                    message = resultado.Mensaje,
                    idUsuario = (resultado.Datos as dynamic)?.id
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ActualizarUsuarioEmpresa(int id, string nombreUsuario, string email, string nombreCompleto,
            string cargo, int empresaId)
        {
            var resultado = _servicioGestion.ActualizarUsuarioEmpresa(
                id, nombreUsuario, email, nombreCompleto, cargo, empresaId);

            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        [HttpPost]
        public JsonResult EliminarUsuarioEmpresa(int id)
        {
            var resultado = _servicioGestion.EliminarUsuarioEmpresa(id);
            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }


        // ===============================
        // EGRESADOS
        // ===============================
        [HttpPost]
        public JsonResult ActualizarInformacionEgresado(
            int idEgresado,
            string numeroDocumento,
            string nombres,
            string apellidos,
            string email,
            string telefono,
            int carrera,
            DateTime fechaGraduacion,
            decimal promedio,
            bool consentimiento,
            string password = null // opcional, solo si se quiere actualizar
        )
        {
            // Generar hash si se proporciona nueva contraseña
            string hash = null;
            if (!string.IsNullOrEmpty(password))
            {
                var auth = new Servicios.AuthService();
                hash = auth.HashPassword(password);
            }

            // Llamada al servicio que actualiza egresado
            var resultado = _servicioGestion.ActualizarEgresado(
                idEgresado,
                numeroDocumento,
                nombres,
                apellidos,
                email,
                telefono,
                carrera,
                fechaGraduacion,
                promedio,
                consentimiento,
                hash
            );

            if (resultado.Exito)
            {
                return Json(new
                {
                    success = true,
                    message = resultado.Mensaje,
                    idEgresado = idEgresado
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult EliminarEgresado(int idEgresado)
        {
            // Llamada al servicio que elimina egresado
            var resultado = _servicioGestion.EliminarEgresado(idEgresado);

            if (resultado.Exito)
            {
                return Json(new
                {
                    success = true,
                    message = resultado.Mensaje,
                    idEgresado = idEgresado
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = resultado.Mensaje }, JsonRequestBehavior.AllowGet);
        }


    }
}
