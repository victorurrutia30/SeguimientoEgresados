using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity; // DbFunctions
using SeguimientoEgresados.Models;
using SeguimientoEgresados.ViewModels;

namespace SeguimientoEgresados.Servicios
{
    public class DashboardService : IDisposable
    {
        private readonly SistemaEgresadosUtecEntities _db;
        private bool _disposed;

        public DashboardService(SistemaEgresadosUtecEntities db = null)
        {
            _db = db ?? new SistemaEgresadosUtecEntities();
        }

        // =========================
        // BÁSICOS
        // =========================
        public int? GetEgresadoIdByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            return _db.Egresados
                      .Where(e => e.email == email)
                      .Select(e => (int?)e.id_egresado)
                      .FirstOrDefault();
        }

        // =========================
        // CONSTRUCTOR DEL DASHBOARD
        // =========================
        public DashboardEgresadoVM BuildDashboard(int idEgresado, int matchingTop = 5, int procesosTake = 8, int notifsTake = 6)
        {
            return new DashboardEgresadoVM
            {
                Resumen = GetResumen(idEgresado),
                CvCard = GetCvCard(idEgresado),
                MatchingTop = GetMatchingTop(idEgresado, matchingTop),
                Procesos = GetProcesos(idEgresado, procesosTake),
                Notificaciones = GetNotificaciones(idEgresado, notifsTake),
                Encuesta = GetEncuestaEstado(idEgresado),
                Evaluaciones = GetEvaluacionesRecientes(idEgresado, 5),
                Estrellas = GetEstrellas(idEgresado),
                EmpresasQueVieron = GetEmpresasQueVieron(idEgresado, 5)
            };
        }

        // =========================
        // SECCIONES / WIDGETS
        // =========================
        public ResumenVM GetResumen(int idEgresado)
        {
            var data = (from e in _db.Egresados
                        join c in _db.Carreras on e.id_carrera equals c.id_carrera into jc
                        from c in jc.DefaultIfEmpty()
                        where e.id_egresado == idEgresado
                        select new
                        {
                            e.id_egresado,
                            e.nombres,
                            e.apellidos,
                            e.email,
                            Carrera = c.nombre_carrera,
                            Facultad = c.facultad,
                            e.fecha_graduacion,     // DateTime (prob. no-nullable en tu EDMX)
                            e.puntuacion_global,    // decimal?
                            e.total_estrellas,      // int o int?
                            e.nivel_experiencia
                        })
                        .FirstOrDefault();

            if (data == null) return null;

            int? mesesDesde = DiffMonthsLikeSql(data.fecha_graduacion, DateTime.Now);


            var totalAplicaciones = _db.Procesos_Seleccion.Count(ps => ps.id_egresado == idEgresado);
            var totalContrataciones = _db.Procesos_Seleccion.Count(ps => ps.id_egresado == idEgresado && ps.contratado == true);

            return new ResumenVM
            {
                IdEgresado = data.id_egresado,
                NombreCompleto = ((data.nombres ?? "").Trim() + " " + (data.apellidos ?? "").Trim()).Trim(),
                Email = data.email,
                Carrera = data.Carrera,
                Facultad = data.Facultad,
                FechaGraduacion = data.fecha_graduacion,
                MesesDesdeGraduacion = mesesDesde,
                PuntuacionGlobal = data.puntuacion_global,
                // Cast a nullable y coalesce para evitar "int? -> int"
                TotalEstrellas = ((int?)data.total_estrellas) ?? 0,
                NivelExperiencia = data.nivel_experiencia,
                TotalAplicaciones = totalAplicaciones,
                TotalContrataciones = totalContrataciones
            };
        }

        public CvCardVM GetCvCard(int idEgresado)
        {
            var cv = _db.CVs_Egresados
                        .Where(c => c.id_egresado == idEgresado)
                        .OrderByDescending(c => c.fecha_actualizacion)
                        .FirstOrDefault();

            if (cv == null)
            {
                return new CvCardVM
                {
                    TieneCv = false,
                    DisponibleBusqueda = false,
                    Privacidad = "Publico",
                    VecesVisualizado = 0
                };
            }

            return new CvCardVM
            {
                TieneCv = true,
                IdCv = cv.id_cv,
                NombreArchivo = cv.nombre_archivo,
                RutaArchivo = cv.ruta_archivo,
                TamanoArchivo = cv.tamaño_archivo,
                FechaSubida = cv.fecha_subida,
                FechaActualizacion = cv.fecha_actualizacion,
                Privacidad = cv.privacidad,
                // Si en EDMX es bool -> ok; si es bool? -> coalesce:
                DisponibleBusqueda = ((bool?)cv.disponible_busqueda) ?? false,
                // Si en EDMX es int -> ok; si es int? -> coalesce:
                VecesVisualizado = ((int?)cv.veces_visualizado) ?? 0,
                ExperienciaAnios = cv.experiencia_años
            };
        }

        public List<MatchingVM> GetMatchingTop(int idEgresado, int top = 5)
        {
            return (from m in _db.Matching_Resultados
                    join emp in _db.Empresas on m.id_empresa equals emp.id_empresa
                    where m.id_egresado == idEgresado
                    orderby m.puntuacion_match descending, m.fecha_calculo descending
                    select new MatchingVM
                    {
                        IdMatching = m.id_matching,
                        IdEmpresa = m.id_empresa,
                        Empresa = emp.razon_social,
                        PuntuacionMatch = m.puntuacion_match,
                        FechaCalculo = m.fecha_calculo,
                        // Normalizamos a bool sólido:
                        VisualizadoEmpresa = (m.visualizado_empresa == true),
                        FechaVisualizacion = m.fecha_visualizacion
                    })
                    .Take(top)
                    .ToList();
        }

        public List<ProcesoVM> GetProcesos(int idEgresado, int take = 8)
        {
            return (from ps in _db.Procesos_Seleccion
                    join emp in _db.Empresas on ps.id_empresa equals emp.id_empresa
                    where ps.id_egresado == idEgresado
                    // Evita '??' si DateTime es no-nullable. Cast a nullable en ambos lados:
                    orderby (((DateTime?)ps.fecha_actualizacion) ?? (DateTime?)ps.fecha_inicio) descending
                    select new ProcesoVM
                    {
                        IdProceso = ps.id_proceso,
                        Empresa = emp.razon_social,
                        TituloVacante = ps.titulo_vacante,
                        EstadoProceso = ps.estado_proceso,
                        // Si es bool o bool? en EDMX, esto lo convierte a bool fijo:
                        Contratado = (ps.contratado == true),
                        FechaInicio = ps.fecha_inicio,
                        FechaActualizacion = ps.fecha_actualizacion,
                        FechaFinalizacion = ps.fecha_finalizacion,
                        SalarioOfrecido = ps.salario_ofrecido
                    })
                    .Take(take)
                    .ToList();
        }

        public List<NotificacionVM> GetNotificaciones(int idEgresado, int take = 6)
        {
            return _db.Notificaciones
                      .Where(n => n.destinatario_tipo == "Egresado" && n.id_destinatario == idEgresado)
                      .OrderByDescending(n => n.fecha_creacion)
                      .Select(n => new NotificacionVM
                      {
                          IdNotificacion = n.id_notificacion,
                          Titulo = n.titulo,
                          Mensaje = n.mensaje,
                          Tipo = n.tipo_notificacion,
                          Fecha = n.fecha_creacion,
                          Leida = (n.leida == true)
                      })
                      .Take(take)
                      .ToList();
        }

        public EncuestaVM GetEncuestaEstado(int idEgresado)
        {
            var e = _db.Encuestas_Base
                       .Where(x => x.id_egresado == idEgresado)
                       .OrderByDescending(x => x.fecha_encuesta)
                       .FirstOrDefault();

            if (e == null) return null;

            return new EncuestaVM
            {
                TrabajandoActualmente = (e.trabajando_actualmente == true),
                EmpresaActual = e.empresa_actual,
                CargoActual = e.cargo_actual,
                SalarioRango = e.salario_rango,
                ModalidadTrabajo = e.modalidad_trabajo,
                UsaConocimientosCarrera = (e.usa_conocimientos_carrera == true),
                SatisfaccionTrabajo = e.satisfaccion_trabajo,
                FechaEncuesta = e.fecha_encuesta
            };
        }

        public List<EvaluacionVM> GetEvaluacionesRecientes(int idEgresado, int take = 5)
        {
            return (from ev in _db.Evaluaciones_Proceso
                    join ps in _db.Procesos_Seleccion on ev.id_proceso equals ps.id_proceso
                    where ps.id_egresado == idEgresado
                    orderby ev.fecha_evaluacion descending
                    select new EvaluacionVM
                    {
                        IdEvaluacion = ev.id_evaluacion,
                        Fase = ev.fase_evaluacion,
                        Puntuacion = ev.puntuacion,
                        PuntuacionMaxima = ev.puntuacion_maxima,
                        Comentarios = ev.comentarios,
                        Evaluador = ev.evaluador,
                        FechaEvaluacion = ev.fecha_evaluacion
                    })
                    .Take(take)
                    .ToList();
        }

        public EstrellasVM GetEstrellas(int idEgresado)
        {
            int total = _db.Egresados
                           .Where(e => e.id_egresado == idEgresado)
                           .Select(e => (int?)e.total_estrellas)
                           .FirstOrDefault() ?? 0;

            var ultimas = _db.Estrellas_Egresado
                             .Where(s => s.id_egresado == idEgresado && (s.completada == true))
                             .OrderByDescending(s => s.fecha_obtencion)
                             .Select(s => new EstrellaDetalleVM
                             {
                                 IdEstrella = s.id_estrella,
                                 // Si estrella_numero es int (no-null), el cast a int? permite usar ?? 0
                                 Numero = ((int?)s.estrella_numero) ?? 0,
                                 FaseCompletada = s.fase_completada,
                                 FechaObtencion = s.fecha_obtencion,
                                 PuntuacionObtenida = s.puntuacion_obtenida
                             })
                             .Take(6)
                             .ToList();

            return new EstrellasVM
            {
                Total = total,
                Ultimas = ultimas
            };
        }

        public List<EmpresaVioVM> GetEmpresasQueVieron(int idEgresado, int take = 5)
        {
            return (from m in _db.Matching_Resultados
                    join emp in _db.Empresas on m.id_empresa equals emp.id_empresa
                    where m.id_egresado == idEgresado
                          && (m.visualizado_empresa == true)
                    orderby m.fecha_visualizacion descending, m.fecha_calculo descending
                    select new EmpresaVioVM
                    {
                        IdEmpresa = m.id_empresa,
                        Empresa = emp.razon_social,
                        FechaVisualizacion = m.fecha_visualizacion
                    })
                    .Take(take)
                    .ToList();
        }

        private static int DiffMonthsLikeSql(DateTime start, DateTime end)
        {
            // Emula DATEDIFF(MONTH, start, end): cuenta cruces de frontera de mes
            return (end.Year - start.Year) * 12 + (end.Month - start.Month);
        }

        // =========================
        // ACCIONES DEL DASHBOARD
        // =========================
        public bool SetDisponibleBusqueda(int idEgresado, bool disponible)
        {
            var cv = _db.CVs_Egresados.FirstOrDefault(c => c.id_egresado == idEgresado);
            if (cv == null) return false;

            cv.disponible_busqueda = disponible;
            cv.fecha_actualizacion = DateTime.Now;
            _db.SaveChanges();
            return true;
        }

        public bool SetPrivacidadCv(int idEgresado, string nivel)
        {
            var permitidos = new[] { "Publico", "SoloEmpresas", "Privado" };
            if (!permitidos.Contains(nivel)) return false;

            var cv = _db.CVs_Egresados.FirstOrDefault(c => c.id_egresado == idEgresado);
            if (cv == null) return false;

            cv.privacidad = nivel;
            cv.fecha_actualizacion = DateTime.Now;
            _db.SaveChanges();
            return true;
        }

        public int MarkAllNotificationsRead(int idEgresado)
        {
            var items = _db.Notificaciones
                           .Where(n => n.destinatario_tipo == "Egresado" && n.id_destinatario == idEgresado)
                           .ToList();

            foreach (var n in items)
            {
                n.leida = true;
                n.fecha_lectura = DateTime.Now;
            }
            _db.SaveChanges();
            return items.Count;
        }

        public bool MarkNotificationRead(int idEgresado, int idNotificacion)
        {
            var n = _db.Notificaciones
                       .FirstOrDefault(x => x.id_notificacion == idNotificacion
                                         && x.destinatario_tipo == "Egresado"
                                         && x.id_destinatario == idEgresado);
            if (n == null) return false;

            n.leida = true;
            n.fecha_lectura = DateTime.Now;
            _db.SaveChanges();
            return true;
        }

        // =========================
        // Limpieza
        // =========================
        public void Dispose()
        {
            if (!_disposed)
            {
                _db.Dispose();
                _disposed = true;
            }
        }
    }
}
