using SeguimientoEgresados.DTO;
using SeguimientoEgresados.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace SeguimientoEgresados.Servicios
{
    public class Utilidades
    {
        private readonly SistemaEgresadosUtecEntities db = new SistemaEgresadosUtecEntities();

        public Resultado ObtenerCarreras()
        {
            try
            {
                var carreras = db.Carreras
                .Where(c => c.activo == true)
                .OrderBy(c => c.nombre_carrera)
                .Select(c => new SelectListItem
                {
                    Value = c.id_carrera.ToString(),
                    Text = c.nombre_carrera
                })
                .ToList();
                if (carreras.Count == 0)
                {
                    return Resultado.error("No se encontraron carreras activas.");
                }
                return Resultado.exito("Carreras", carreras);
            }
            catch (Exception ex)
            {
                return Resultado.error(ex.Message);
            }
        }
    }
}