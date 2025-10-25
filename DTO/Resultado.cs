namespace SeguimientoEgresados.DTO
{
    public class Resultado
    {
        public bool Exito { get; }
        public string Mensaje { get; }
        public object Datos { get; }
        public Resultado(bool exito, string mensaje = null, object datos = null)
        {
            Exito = exito;
            Mensaje = mensaje;
            Datos = datos;
        }

        public static Resultado exito(string mensaje = null, object datos = null)
        {
            return new Resultado(true, mensaje, datos);
        }
        public static Resultado error(string mensaje = null, object datos = null)
        {
            return new Resultado(false, mensaje, datos);
        }
    }
}