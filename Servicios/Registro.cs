using SeguimientoEgresados.DTO;
using SeguimientoEgresados.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;

namespace SeguimientoEgresados.Servicios
{
    public class Registro
    {
        private readonly SistemaEgresadosUtecEntities _db = new SistemaEgresadosUtecEntities();

        public Resultado RegistrarEgresado(string numeroDocumento, string nombres, string apellidos,
    string email, string telefono, int carrera, DateTime fechaGraduaacion, decimal promedio, bool consentimiento,
    HttpPostedFileBase CV, int experiencia, string habilidades, string idiomas, string certificaciones,
    string hashedPassword)
        {
            try
            {
                var existe = _db.Egresados.Any(e => e.numero_documento == numeroDocumento || e.email == email);
                if (existe)
                    return Resultado.error("Ya existe un egresado con el mismo número de documento o correo electrónico.");

                var nuevoEgresado = new Egresado
                {
                    numero_documento = numeroDocumento,
                    nombres = nombres,
                    apellidos = apellidos,
                    email = email,
                    telefono = telefono,
                    id_carrera = carrera,
                    fecha_graduacion = fechaGraduaacion,
                    promedio_academico = promedio,
                    consentimiento_datos = consentimiento,
                    fecha_registro = DateTime.Now,
                    puntuacion_global = 0,
                    total_estrellas = 0,
                    nivel_experiencia = "Indefinido",
                    password_hash = hashedPassword
                };

                _db.Egresados.Add(nuevoEgresado);
                _db.SaveChanges();

                // CV opcional
                if (CV != null && CV.ContentLength > 0)
                {
                    var Cv = new CVs_Egresados()
                    {
                        id_egresado = nuevoEgresado.id_egresado,
                        nombre_archivo = CV.FileName,
                        ruta_archivo = CV.ContentType,
                        tamaño_archivo = CV.ContentLength,
                        experiencia_años = experiencia,
                        habilidades_principales = habilidades,
                        idiomas = idiomas,
                        certificaciones = certificaciones,
                        disponible_busqueda = true,
                        fecha_subida = DateTime.Now,
                        fecha_actualizacion = DateTime.Now,
                        veces_visualizado = 0
                    };
                    _db.CVs_Egresados.Add(Cv);
                    _db.SaveChanges();
                }

                // 👉 devolvemos el id, lo necesita el paso 2 (encuesta)
                return Resultado.exito("Egresado registrado exitosamente.", new { id = nuevoEgresado.id_egresado });
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al registrar egresado: " + ex.Message);
            }
        }


        public Resultado GuardarCV(int idEgresado, HttpPostedFileBase CV, int experiencia, string habilidades, string idiomas, string certificaciones)
        {
            try
            {
                if (CV == null || CV.ContentLength == 0)
                {
                    return Resultado.error("No se ha proporcionado un archivo CV válido.");
                }

                var egresado = _db.Egresados.Find(idEgresado);
                if (egresado == null)
                {
                    return Resultado.error("Egresado no encontrado.");
                }

                var Cv = new CVs_Egresados()
                {
                    id_egresado = idEgresado,
                    nombre_archivo = CV.FileName,
                    ruta_archivo = CV.ContentType,
                    tamaño_archivo = CV.ContentLength,
                    experiencia_años = experiencia,
                    habilidades_principales = habilidades,
                    idiomas = idiomas,
                    certificaciones = certificaciones,
                    disponible_busqueda = true,
                    fecha_subida = DateTime.Now,
                    fecha_actualizacion = DateTime.Now,
                    veces_visualizado = 0
                };

                _db.CVs_Egresados.Add(Cv);
                _db.SaveChanges();
                return Resultado.exito("CV guardado exitosamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al guardar CV: " + ex.Message);
            }
        }

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

                    // nuevos
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

        public Resultado GuardarSituacionLaboral_NoTrabaja(
            int idEgresado, HttpPostedFileBase CV,
            string contactaUniversidad, byte? deseaContacto, byte? dispuestoEncuestaSemestral,
            string metodoInicioSesion, string respuestasJson, string sugerenciaFuncionalidad)
        {
            try
            {
                var egresado = _db.Egresados.Find(idEgresado);
                if (egresado == null) return Resultado.error("Egresado no encontrado.");

                // CV ya pudo haberse subido en el registro; aquí lo dejamos opcional
                if (CV != null && CV.ContentLength > 0)
                {
                    if (!CV.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        return Resultado.error("El CV debe ser un archivo PDF.");

                    if (CV.ContentLength > 5 * 1024 * 1024)
                        return Resultado.error("El CV no puede superar los 5 MB.");

                    string rutaCV = GuardarArchivoCV(CV);
                    if (string.IsNullOrEmpty(rutaCV))
                        return Resultado.error("Error al guardar el archivo CV.");

                    var cvEgresado = new CVs_Egresados
                    {
                        id_egresado = idEgresado,
                        ruta_archivo = rutaCV,
                        nombre_archivo = CV.FileName,
                        fecha_subida = DateTime.Now
                    };
                    _db.CVs_Egresados.Add(cvEgresado);
                }

                var encuesta = new Encuestas_Base
                {
                    id_egresado = idEgresado,
                    trabajando_actualmente = false,
                    fecha_encuesta = DateTime.Now,

                    // nuevos
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

        // Unificado
        public Resultado GuardarSituacionLaboral(
            int idEgresado, bool trabajandoActualmente,
            string empresa = null, string cargo = null, string salario = null, string modalidad = null,
            byte? satisfaccion = null, bool? usaConocimientos = null, int? tiempoConseguirTrabajo = null,
            HttpPostedFileBase CV = null,
            // nuevos
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

        // 📂 Guardar archivo físicamente
        private string GuardarArchivoCV(HttpPostedFileBase CV)
        {
            try
            {
                string fileName = $"{Guid.NewGuid()}_{CV.FileName}";
                string path = HttpContext.Current.Server.MapPath("~/Uploads/CV/");
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);

                string fullPath = System.IO.Path.Combine(path, fileName);
                CV.SaveAs(fullPath);

                return "/Uploads/CV/" + fileName;
            }
            catch
            {
                return null;
            }
        }
    }
}
