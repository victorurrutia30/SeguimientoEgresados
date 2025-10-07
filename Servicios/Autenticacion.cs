using SeguimientoEgresados.DTO;
using SeguimientoEgresados.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SeguimientoEgresados.Servicios
{
    public class AuthService
    {
        private readonly SistemaEgresadosUtecEntities _db = new SistemaEgresadosUtecEntities();

        public bool Login(string email, string password, out string rol, out int userId)
        {
            rol = null;
            userId = 0;

            var admin = _db.Administradores.FirstOrDefault(a => a.email == email && a.activo == true);
            if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.password_hash))
            {
                rol = "Admin";
                userId = admin.id_admin;
                return true;
            }

            var egresado = _db.Egresados.FirstOrDefault(e => e.email == email && e.estado_activo == true);
            if (egresado != null && BCrypt.Net.BCrypt.Verify(password, egresado.password_hash))
            {
                rol = "Egresado";
                userId = egresado.id_egresado;
                return true;
            }

            var empresa = _db.Usuarios_Empresa.FirstOrDefault(u => u.email == email && u.activo == true);
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