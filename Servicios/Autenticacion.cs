using SeguimientoEgresados.Models;
using System.Linq;

namespace SeguimientoEgresados.Servicios
{
    public class AuthService
    {
        private readonly SistemaEgresadosUtecEntities _db = new SistemaEgresadosUtecEntities();

        /// <summary>
        /// Valida credenciales. Devuelve rol y userId si es correcto.
        /// </summary>
        public bool Login(string email, string password, out string rol, out int userId)
        {
            rol = null;
            userId = 0;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

            var emailNorm = email.Trim().ToLower();

            // Admin
            var admin = _db.Administradores
                .FirstOrDefault(a => a.activo == true && a.email.ToLower() == emailNorm);
            if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.password_hash))
            {
                rol = "Admin";
                userId = admin.id_admin;
                return true;
            }

            // Egresado
            var egresado = _db.Egresados
                .FirstOrDefault(e => e.estado_activo == true && e.email.ToLower() == emailNorm);
            if (egresado != null && BCrypt.Net.BCrypt.Verify(password, egresado.password_hash))
            {
                rol = "Egresado";
                userId = egresado.id_egresado;
                return true;
            }

            // Empresa
            var empresa = _db.Usuarios_Empresa
                .FirstOrDefault(u => u.activo == true && u.email.ToLower() == emailNorm);
            if (empresa != null && BCrypt.Net.BCrypt.Verify(password, empresa.password_hash))
            {
                rol = "Empresa";
                userId = empresa.id_usuario;
                return true;
            }

            return false;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
