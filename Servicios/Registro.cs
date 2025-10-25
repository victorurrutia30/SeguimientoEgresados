using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.Entity.Validation;
using SeguimientoEgresados.DTO;
using SeguimientoEgresados.Models;

namespace SeguimientoEgresados.Servicios
{
    public class Registro
    {
        private readonly SistemaEgresadosUtecEntities _db = new SistemaEgresadosUtecEntities();

        // Valor por defecto para cumplir con la validación NOT NULL del campo 'privacidad'
        private const string DEFAULT_PRIVACIDAD = "Publico"; // Cambia a "Privado" si lo deseas

        // ---------------------------------------------------------------------
        // Util: genera un nombre de archivo seguro, corto y único
        // ---------------------------------------------------------------------
        private static string NombreArchivoSeguro(string original, int baseMax = 80)
        {
            var justName = Path.GetFileName(string.IsNullOrWhiteSpace(original) ? "cv.pdf" : original);
            var baseName = Path.GetFileNameWithoutExtension(justName) ?? "cv";
            var ext = Path.GetExtension(justName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".pdf";

            var invalid = Path.GetInvalidFileNameChars();
            var limpio = new string(baseName.Where(c => !invalid.Contains(c)).ToArray());

            if (limpio.Length > baseMax) limpio = limpio.Substring(0, baseMax);

            return $"{Guid.NewGuid():N}_{limpio}{ext}";
        }

        // ---------------------------------------------------------------------
        // Guarda archivo físicamente. Devuelve ruta virtual y saca por out el nombre guardado.
        // ---------------------------------------------------------------------
        private string GuardarArchivoCV(HttpPostedFileBase CV, out string nombreGuardado)
        {
            nombreGuardado = null;
            try
            {
                var safeName = NombreArchivoSeguro(CV.FileName);
                var path = HttpContext.Current.Server.MapPath("~/Uploads/CV/");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                var fullPath = Path.Combine(path, safeName);
                CV.SaveAs(fullPath);

                nombreGuardado = safeName;
                return "/Uploads/CV/" + safeName;
            }
            catch
            {
                return null;
            }
        }

        // ---------------------------------------------------------------------
        // Alta de egresado + (opcional) CV en la MISMA transacción
        // ---------------------------------------------------------------------
        public Resultado RegistrarEgresado(
            string numeroDocumento, string nombres, string apellidos,
            string email, string telefono, int carrera, DateTime fechaGraduacion, decimal promedio, bool consentimiento,
            HttpPostedFileBase CV, int experiencia, string habilidades, string idiomas, string certificaciones,
            string hashedPassword)
        {
            try
            {
                var existe = _db.Egresados.Any(e => e.numero_documento == numeroDocumento || e.email == email);
                if (existe)
                    return Resultado.error("Ya existe un egresado con el mismo número de documento o correo electrónico.");

                using (var tx = _db.Database.BeginTransaction())
                {
                    var nuevoEgresado = new Egresado
                    {
                        numero_documento = numeroDocumento,
                        nombres = nombres,
                        apellidos = apellidos,
                        email = email,
                        telefono = telefono,
                        id_carrera = carrera,
                        fecha_graduacion = fechaGraduacion,
                        promedio_academico = promedio,
                        consentimiento_datos = consentimiento,
                        fecha_registro = DateTime.Now,
                        puntuacion_global = 0,
                        total_estrellas = 0,
                        nivel_experiencia = "Indefinido",
                        password_hash = hashedPassword
                    };

                    _db.Egresados.Add(nuevoEgresado);
                    _db.SaveChanges(); // 1) guarda egresado

                    // CV opcional
                    if (CV != null && CV.ContentLength > 0)
                    {
                        if (!CV.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                            return Resultado.error("El CV debe ser un archivo PDF.");
                        if (CV.ContentLength > 5 * 1024 * 1024)
                            return Resultado.error("El CV no puede superar los 5 MB.");

                        var rutaCV = GuardarArchivoCV(CV, out var safeName);
                        if (string.IsNullOrEmpty(rutaCV))
                            return Resultado.error("Error al guardar el archivo CV.");

                        var cv = new CVs_Egresados
                        {
                            id_egresado = nuevoEgresado.id_egresado,
                            nombre_archivo = safeName,            // nombre corto/seguro
                            ruta_archivo = rutaCV,                // ruta virtual para servir el archivo
                            tamaño_archivo = CV.ContentLength,
                            experiencia_años = experiencia,
                            habilidades_principales = string.IsNullOrWhiteSpace(habilidades) ? null : habilidades,
                            idiomas = string.IsNullOrWhiteSpace(idiomas) ? null : idiomas,
                            certificaciones = string.IsNullOrWhiteSpace(certificaciones) ? null : certificaciones,
                            disponible_busqueda = true,
                            fecha_subida = DateTime.Now,
                            fecha_actualizacion = DateTime.Now,
                            veces_visualizado = 0,
                            privacidad = DEFAULT_PRIVACIDAD       // ← IMPORTANTE
                            // tipo_mime = CV.ContentType // descomenta si existe y es requerido
                        };

                        _db.CVs_Egresados.Add(cv);
                        _db.SaveChanges(); // 2) guarda CV
                    }

                    tx.Commit();

                    return Resultado.exito("Egresado registrado exitosamente.", new { id = nuevoEgresado.id_egresado });
                }
            }
            catch (DbEntityValidationException ex)
            {
                var detalles = string.Join(" | ",
                    ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors)
                      .Select(v => $"{v.PropertyName}: {v.ErrorMessage}"));
                return Resultado.error("Validación fallida (Egresado/CV): " + detalles);
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al registrar egresado: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // Guardado explícito del CV (por si hay flujo separado)
        // ---------------------------------------------------------------------
        public Resultado GuardarCV(int idEgresado, HttpPostedFileBase CV, int experiencia, string habilidades, string idiomas, string certificaciones)
        {
            try
            {
                if (CV == null || CV.ContentLength == 0)
                    return Resultado.error("No se ha proporcionado un archivo CV válido.");

                var egresado = _db.Egresados.Find(idEgresado);
                if (egresado == null)
                    return Resultado.error("Egresado no encontrado.");

                if (!CV.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return Resultado.error("El CV debe ser un archivo PDF.");
                if (CV.ContentLength > 5 * 1024 * 1024)
                    return Resultado.error("El CV no puede superar los 5 MB.");

                var rutaCV = GuardarArchivoCV(CV, out var safeName);
                if (string.IsNullOrEmpty(rutaCV))
                    return Resultado.error("Error al guardar el archivo CV.");

                var cv = new CVs_Egresados
                {
                    id_egresado = idEgresado,
                    nombre_archivo = safeName,           // nombre guardado
                    ruta_archivo = rutaCV,               // ruta virtual
                    tamaño_archivo = CV.ContentLength,
                    experiencia_años = experiencia,
                    habilidades_principales = string.IsNullOrWhiteSpace(habilidades) ? null : habilidades,
                    idiomas = string.IsNullOrWhiteSpace(idiomas) ? null : idiomas,
                    certificaciones = string.IsNullOrWhiteSpace(certificaciones) ? null : certificaciones,
                    disponible_busqueda = true,
                    fecha_subida = DateTime.Now,
                    fecha_actualizacion = DateTime.Now,
                    veces_visualizado = 0,
                    privacidad = DEFAULT_PRIVACIDAD      // ← IMPORTANTE
                    // tipo_mime = CV.ContentType  // descomenta si existe y es requerido
                };

                _db.CVs_Egresados.Add(cv);
                _db.SaveChanges();
                return Resultado.exito("CV guardado exitosamente.");
            }
            catch (DbEntityValidationException ex)
            {
                var detalles = string.Join(" | ",
                    ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors)
                      .Select(v => $"{v.PropertyName}: {v.ErrorMessage}"));
                return Resultado.error("Validación de CV fallida: " + detalles);
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al guardar CV: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // Encuesta: rama “Sí trabaja”
        // ---------------------------------------------------------------------
        public Resultado GuardarSituacionLaboral_Trabaja(
            int idEgresado, string empresaActual, string cargoActual, string rangoSalarial, string modalidadTrabajo,
            byte? satisfaccionTrabajo, bool? usaConocimientosCarrera, int? tiempoConseguirTrabajo,
            string contactaUniversidad, byte? deseaContacto, byte? dispuestoEncuestaSemestral,
            string metodoInicioSesion, string respuestasJson, string sugerenciaFuncionalidad)
        {
            try
            {
                var egresado = _db.Egresados.Find(idEgresado);
                if (egresado == null) return Resultado.error("Egresado no encontrado.");

                var encuesta = new Encuestas_Base
                {
                    id_egresado = idEgresado,
                    trabajando_actualmente = true,
                    empresa_actual = empresaActual,
                    cargo_actual = cargoActual,
                    salario_rango = rangoSalarial,
                    modalidad_trabajo = modalidadTrabajo,
                    satisfaccion_trabajo = satisfaccionTrabajo,
                    usa_conocimientos_carrera = usaConocimientosCarrera,
                    tiempo_conseguir_trabajo = tiempoConseguirTrabajo,
                    fecha_encuesta = DateTime.Now,

                    contacta_universidad = contactaUniversidad,
                    desea_contacto = deseaContacto,
                    dispuesto_encuesta_semestral = dispuestoEncuestaSemestral,
                    metodo_inicio_sesion = metodoInicioSesion,
                    respuestas_json = respuestasJson,
                    sugerencia_funcionalidad = sugerenciaFuncionalidad
                };

                _db.Encuestas_Base.Add(encuesta);
                _db.SaveChanges();

                return Resultado.exito("Encuesta (sí trabaja) guardada correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al guardar la encuesta laboral: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // Encuesta: rama “No trabaja” (CV opcional aquí)
        // ---------------------------------------------------------------------
        public Resultado GuardarSituacionLaboral_NoTrabaja(
            int idEgresado, HttpPostedFileBase CV,
            string contactaUniversidad, byte? deseaContacto, byte? dispuestoEncuestaSemestral,
            string metodoInicioSesion, string respuestasJson, string sugerenciaFuncionalidad)
        {
            try
            {
                var egresado = _db.Egresados.Find(idEgresado);
                if (egresado == null) return Resultado.error("Egresado no encontrado.");

                // Si adjuntó CV en este paso, lo guardamos
                if (CV != null && CV.ContentLength > 0)
                {
                    if (!CV.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        return Resultado.error("El CV debe ser un archivo PDF.");
                    if (CV.ContentLength > 5 * 1024 * 1024)
                        return Resultado.error("El CV no puede superar los 5 MB.");

                    var rutaCV = GuardarArchivoCV(CV, out var safeName);
                    if (string.IsNullOrEmpty(rutaCV))
                        return Resultado.error("Error al guardar el archivo CV.");

                    var cvEgresado = new CVs_Egresados
                    {
                        id_egresado = idEgresado,
                        nombre_archivo = safeName,
                        ruta_archivo = rutaCV,
                        tamaño_archivo = CV.ContentLength,
                        disponible_busqueda = true,
                        fecha_subida = DateTime.Now,
                        fecha_actualizacion = DateTime.Now,
                        veces_visualizado = 0,
                        privacidad = DEFAULT_PRIVACIDAD      // ← IMPORTANTE
                        // tipo_mime = CV.ContentType  // descomenta si existe y es requerido
                    };
                    _db.CVs_Egresados.Add(cvEgresado);
                }

                var encuesta = new Encuestas_Base
                {
                    id_egresado = idEgresado,
                    trabajando_actualmente = false,
                    fecha_encuesta = DateTime.Now,

                    contacta_universidad = contactaUniversidad,
                    desea_contacto = deseaContacto,
                    dispuesto_encuesta_semestral = dispuestoEncuestaSemestral,
                    metodo_inicio_sesion = metodoInicioSesion,
                    respuestas_json = respuestasJson,
                    sugerencia_funcionalidad = sugerenciaFuncionalidad
                };
                _db.Encuestas_Base.Add(encuesta);

                _db.SaveChanges();

                return Resultado.exito("Encuesta (no trabaja) guardada correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al guardar la encuesta: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // Facade unificado
        // ---------------------------------------------------------------------
        public Resultado GuardarSituacionLaboral(
            int idEgresado, bool trabajandoActualmente,
            string empresa = null, string cargo = null, string salario = null, string modalidad = null,
            byte? satisfaccion = null, bool? usaConocimientos = null, int? tiempoConseguirTrabajo = null,
            HttpPostedFileBase CV = null,
            string contactaUniversidad = null, byte? deseaContacto = null, byte? dispuestoEncuestaSemestral = null,
            string metodoInicioSesion = null, string respuestasJson = null, string sugerenciaFuncionalidad = null)
        {
            if (trabajandoActualmente)
            {
                return GuardarSituacionLaboral_Trabaja(
                    idEgresado, empresa, cargo, salario, modalidad, satisfaccion, usaConocimientos, tiempoConseguirTrabajo,
                    contactaUniversidad, deseaContacto, dispuestoEncuestaSemestral,
                    metodoInicioSesion, respuestasJson, sugerenciaFuncionalidad
                );
            }
            else
            {
                return GuardarSituacionLaboral_NoTrabaja(
                    idEgresado, CV,
                    contactaUniversidad, deseaContacto, dispuestoEncuestaSemestral,
                    metodoInicioSesion, respuestasJson, sugerenciaFuncionalidad
                );
            }
        }
    }
}
