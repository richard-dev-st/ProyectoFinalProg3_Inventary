namespace Core.Domain.Entities
{
    public class Usuario
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Rol { get; private set; } = "Estandar"; //Administrador o Estandar
        public bool Activo { get; private set; } = false;

        //Tokens de Activacion
        public string? TokenActivacion { get; private set; }
        public DateTime? TokenActivacionExpiracion { get; private set; }

        private Usuario() { } // Constructor privado para EF Core)

        public Usuario(string email, string passwordHash)
        {
            Id = Guid.NewGuid();
            Email = email.ToLowerInvariant().Trim();
            PasswordHash = passwordHash;
            Activo = false;
            GenerarTokenActivacion();
        }

        public void GenerarTokenActivacion()
        {
            TokenActivacion = Guid.NewGuid().ToString("N");
            TokenActivacionExpiracion = DateTime.UtcNow.AddHours(24); // Token válido por 24 horas
        }

        public bool ActivarCuenta(string token)
        {
            if (Activo)
            {
                return false; // La cuenta ya está activa
            }
            if(TokenActivacion != token) 
            {
                return false; // Token inválido
            }
            if(TokenActivacionExpiracion == null || DateTime.UtcNow > TokenActivacionExpiracion) 
            {
                return false; // Token expirado
            }

            Activo = true; // Activar la cuenta
            TokenActivacion = null; // Limpiar el token
            TokenActivacionExpiracion = null; // Limpiar la expiración del token
            return true; // Activación exitosa
        }
    }
}
