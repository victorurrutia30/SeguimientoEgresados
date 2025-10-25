using System;
using System.Collections.Generic;

namespace SeguimientoEgresados.ViewModels
{
    // Declaro primero los tipos usados por DashboardEgresadoVM para evitar errores de referencia
    public class ResumenVM
    {
        public int IdEgresado { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string Carrera { get; set; }
        public string Facultad { get; set; }
        public DateTime FechaGraduacion { get; set; }
        public int? MesesDesdeGraduacion { get; set; }
        public decimal? PuntuacionGlobal { get; set; }
        public int TotalEstrellas { get; set; }
        public string NivelExperiencia { get; set; }
        public int TotalAplicaciones { get; set; }
        public int TotalContrataciones { get; set; }
    }

    public class CvCardVM
    {
        public bool TieneCv { get; set; }
        public int? IdCv { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaArchivo { get; set; }
        public long? TamanoArchivo { get; set; }
        public DateTime? FechaSubida { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public string Privacidad { get; set; }
        public bool DisponibleBusqueda { get; set; }
        public int VecesVisualizado { get; set; }
        public decimal? ExperienciaAnios { get; set; }
    }

    public class MatchingVM
    {
        public int IdMatching { get; set; }
        public int IdEmpresa { get; set; }
        public string Empresa { get; set; }
        public decimal? PuntuacionMatch { get; set; }
        public DateTime? FechaCalculo { get; set; }
        public bool VisualizadoEmpresa { get; set; }
        public DateTime? FechaVisualizacion { get; set; }
    }

    public class ProcesoVM
    {
        public int IdProceso { get; set; }
        public string Empresa { get; set; }
        public string TituloVacante { get; set; }
        public string EstadoProceso { get; set; }
        public bool Contratado { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public decimal? SalarioOfrecido { get; set; }
    }

    public class NotificacionVM
    {
        public int IdNotificacion { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string Tipo { get; set; }
        public DateTime? Fecha { get; set; }
        public bool Leida { get; set; }
    }

    public class EncuestaVM
    {
        public bool TrabajandoActualmente { get; set; }
        public string EmpresaActual { get; set; }
        public string CargoActual { get; set; }
        public string SalarioRango { get; set; }
        public string ModalidadTrabajo { get; set; }
        public bool UsaConocimientosCarrera { get; set; }
        public byte? SatisfaccionTrabajo { get; set; }
        public DateTime? FechaEncuesta { get; set; }
    }

    public class EstrellasVM
    {
        public int Total { get; set; }
        public List<EstrellaDetalleVM> Ultimas { get; set; }
    }

    public class EstrellaDetalleVM
    {
        public int IdEstrella { get; set; }
        public int Numero { get; set; }
        public string FaseCompletada { get; set; }
        public DateTime? FechaObtencion { get; set; }
        public decimal? PuntuacionObtenida { get; set; }
    }

    public class EmpresaVioVM
    {
        public int IdEmpresa { get; set; }
        public string Empresa { get; set; }
        public DateTime? FechaVisualizacion { get; set; }
    }

    public class EvaluacionVM
    {
        public int IdEvaluacion { get; set; }
        public string Fase { get; set; }
        public decimal? Puntuacion { get; set; }
        public decimal? PuntuacionMaxima { get; set; }
        public string Comentarios { get; set; }
        public string Evaluador { get; set; }
        public DateTime? FechaEvaluacion { get; set; }
    }

    public class DashboardEgresadoVM
    {
        public ResumenVM Resumen { get; set; }
        public CvCardVM CvCard { get; set; }
        public List<MatchingVM> MatchingTop { get; set; }
        public List<ProcesoVM> Procesos { get; set; }
        public List<NotificacionVM> Notificaciones { get; set; }
        public EncuestaVM Encuesta { get; set; }
        public List<EvaluacionVM> Evaluaciones { get; set; }
        public EstrellasVM Estrellas { get; set; }
        public List<EmpresaVioVM> EmpresasQueVieron { get; set; }
    }
}
