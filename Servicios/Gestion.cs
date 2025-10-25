using SeguimientoEgresados.DTO;
using SeguimientoEgresados.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeguimientoEgresados.Servicios
{
    public class Gestion
    {
        private readonly SistemaEgresadosUtecEntities _db = new SistemaEgresadosUtecEntities();

        // ===============================
        // EMPRESAS
        // ===============================
        public Resultado CrearEmpresa(string razonSocial, string nit, string emailContacto, string telefono,
            string direccion, string sectorEconomico, string tamañoEmpresa, bool vinculadaUniversidad = true,
            DateTime? fechaRegistro = null, bool estadoActivo = true, decimal puntuacionEmpresa = 5.0m,
            int totalContrataciones = 0)
        {
            try
            {
                if (_db.Empresas.Any(e => e.nit == nit))
                    return Resultado.error("Ya existe una empresa con ese NIT.");

                var empresa = new Empresa
                {
                    razon_social = razonSocial,
                    nit = nit,
                    email_contacto = emailContacto,
                    telefono = telefono,
                    direccion = direccion,
                    sector_economico = sectorEconomico,
                    tamaño_empresa = tamañoEmpresa,
                    vinculada_universidad = vinculadaUniversidad,
                    fecha_registro = fechaRegistro ?? DateTime.Now,
                    estado_activo = true,
                    puntuacion_empresa = 5.0m,
                    total_contrataciones = 0
                };

                _db.Empresas.Add(empresa);
                _db.SaveChanges();

                return Resultado.exito("Empresa creada correctamente.", new { id = empresa.id_empresa });
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al crear la empresa: " + ex.Message);
            }
        }

        public Resultado ActualizarEmpresa(int id, string razonSocial, string nit, string emailContacto, string telefono,
            string direccion, string sectorEconomico, string tamañoEmpresa, bool vinculadaUniversidad, DateTime? fechaRegistro = null,
            bool estadoActivo = true, decimal puntuacionEmpresa = 5.0m, int totalContrataciones = 0)
        {
            try
            {
                var empresa = _db.Empresas.Find(id);
                if (empresa == null) return Resultado.error("Empresa no encontrada.");

                empresa.razon_social = razonSocial;
                empresa.nit = nit;
                empresa.email_contacto = emailContacto;
                empresa.telefono = telefono;
                empresa.direccion = direccion;
                empresa.sector_economico = sectorEconomico;
                empresa.tamaño_empresa = tamañoEmpresa;
                empresa.vinculada_universidad = vinculadaUniversidad;
                empresa.fecha_registro = fechaRegistro ?? empresa.fecha_registro;
                empresa.estado_activo = estadoActivo;
                empresa.puntuacion_empresa = puntuacionEmpresa;
                empresa.total_contrataciones = totalContrataciones;

                _db.SaveChanges();
                return Resultado.exito("Empresa actualizada correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al actualizar la empresa: " + ex.Message);
            }
        }


        public Resultado EliminarEmpresa(int id)
        {
            try
            {
                var empresa = _db.Empresas.Find(id);
                if (empresa == null) return Resultado.error("Empresa no encontrada.");

                _db.Empresas.Remove(empresa);
                _db.SaveChanges();
                return Resultado.exito("Empresa eliminada correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al eliminar la empresa: " + ex.Message);
            }
        }

        public List<Empresa> ObtenerEmpresas()
        {
            return _db.Empresas.ToList();
        }

        // ===============================
        // USUARIOS DE EMPRESA
        // ===============================
        public Resultado CrearUsuarioEmpresa(
            int empresaId,
            string nombreUsuario,
            string email,
            string passwordHash,
            string nombreCompleto,
            string cargo = null)
        {
            try
            {
                if (_db.Usuarios_Empresa.Any(u => u.nombre_usuario == nombreUsuario))
                    return Resultado.error("Ya existe un usuario con ese nombre de usuario.");

                if (_db.Usuarios_Empresa.Any(u => u.email == email))
                    return Resultado.error("Ya existe un usuario con ese correo.");

                var usuario = new Usuarios_Empresa
                {
                    id_empresa = empresaId,
                    nombre_usuario = nombreUsuario,
                    email = email,
                    password_hash = passwordHash,
                    nombre_completo = nombreCompleto,
                    cargo = cargo,
                    activo = true,
                    fecha_creacion = DateTime.Now
                };

                _db.Usuarios_Empresa.Add(usuario);
                _db.SaveChanges();
                return Resultado.exito("Usuario de empresa creado correctamente.", new { id = usuario.id_usuario });
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al crear usuario: " + ex.Message);
            }
        }

        public Resultado ActualizarUsuarioEmpresa(
            int id,
            string nombreUsuario,
            string email,
            string nombreCompleto,
            string cargo,
            int empresaId)
        {
            try
            {
                var usuario = _db.Usuarios_Empresa.Find(id);
                if (usuario == null) return Resultado.error("Usuario no encontrado.");

                usuario.nombre_usuario = nombreUsuario;
                usuario.email = email;
                usuario.nombre_completo = nombreCompleto;
                usuario.cargo = cargo;
                usuario.id_empresa = empresaId;

                _db.SaveChanges();
                return Resultado.exito("Usuario de empresa actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al actualizar usuario: " + ex.Message);
            }
        }

        public Resultado EliminarUsuarioEmpresa(int id)
        {
            try
            {
                var usuario = _db.Usuarios_Empresa.Find(id);
                if (usuario == null) return Resultado.error("Usuario no encontrado.");

                _db.Usuarios_Empresa.Remove(usuario);
                _db.SaveChanges();
                return Resultado.exito("Usuario de empresa eliminado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al eliminar usuario: " + ex.Message);
            }
        }

        public List<Usuarios_Empresa> ObtenerUsuariosEmpresa(int? empresaId = null)
        {
            if (empresaId.HasValue)
                return _db.Usuarios_Empresa.Where(u => u.id_empresa == empresaId.Value).ToList();
            return _db.Usuarios_Empresa.ToList();
        }

        // ===============================
        // EGRESADOS
        // ===============================
        public Resultado ActualizarEgresado(
            int idEgresado,
            string numeroDocumento,
            string nombres,
            string apellidos,
            string email,
            string telefono,
            int idCarrera,
            DateTime fechaGraduacion,
            decimal promedioAcademico,
            bool consentimientoDatos,
            string passwordHash = null // opcional
        )
        {
            try
            {
                var egresado = _db.Egresados.Find(idEgresado);
                if (egresado == null) return Resultado.error("Egresado no encontrado.");

                egresado.numero_documento = numeroDocumento;
                egresado.nombres = nombres;
                egresado.apellidos = apellidos;
                egresado.email = email;
                egresado.telefono = telefono;
                egresado.id_carrera = idCarrera;
                egresado.fecha_graduacion = fechaGraduacion;
                egresado.promedio_academico = promedioAcademico;
                egresado.consentimiento_datos = consentimientoDatos;

                if (!string.IsNullOrEmpty(passwordHash))
                {
                    egresado.password_hash = passwordHash;
                }

                _db.SaveChanges();
                return Resultado.exito("Egresado actualizado correctamente.", new { id = idEgresado });
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al actualizar egresado: " + ex.Message);
            }
        }
        public Resultado EliminarEgresado(int idEgresado)
        {
            try
            {
                var egresado = _db.Egresados.Find(idEgresado);
                if (egresado == null) return Resultado.error("Egresado no encontrado.");

                _db.Egresados.Remove(egresado);
                _db.SaveChanges();

                return Resultado.exito("Egresado eliminado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.error("Error al eliminar egresado: " + ex.Message);
            }
        }

        public List<Egresado> ObtenerEgresados()
        {
            return _db.Egresados.ToList();
        }
    }
}
