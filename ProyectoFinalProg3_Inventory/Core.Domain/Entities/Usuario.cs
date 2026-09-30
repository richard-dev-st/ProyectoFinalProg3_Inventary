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

        //Intentos de inicio de sesión fallidos
        public int IntentosFallidos { get; private set; } = 0;

        //Bloqueo de cuenta
        public DateTime? BloqueadoHasta { get; private set; }

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
            if (TokenActivacion != token)
            {
                return false; // Token inválido
            }
            if (TokenActivacionExpiracion == null || DateTime.UtcNow > TokenActivacionExpiracion)
            {
                return false; // Token expirado
            }

            Activo = true; // Activar la cuenta
            TokenActivacion = null; // Limpiar el token
            TokenActivacionExpiracion = null; // Limpiar la expiración del token
            return true; // Activación exitosa
        }

        //Metodo que se encarga de revisar si la cuenta está bloqueada y manejar el desbloqueo si es necesario
        public bool EstaBloqueado()
        {
            // Verificar si la cuenta está bloqueada
            if (BloqueadoHasta.HasValue)
            {
                // Si la fecha de bloqueo aún no ha pasado, la cuenta sigue bloqueada
                if (DateTime.UtcNow < BloqueadoHasta.Value)
                {
                    return true; // La cuenta está bloqueada
                }
                BloqueadoHasta = null; // Limpiar el bloqueo si ha expirado
                IntentosFallidos = 0; // Reiniciar los intentos fallidos
            }

            return false; // La cuenta no está bloqueada
        }

        public void RegistrarIntentoFallido()
        {
            // Incrementar el contador de intentos fallidos
            IntentosFallidos++;

            // Si se alcanzan 5 intentos fallidos, bloquear la cuenta por 15 minutos
            if (IntentosFallidos >= 5)
            {
                BloqueadoHasta = DateTime.UtcNow.AddMinutes(15); // Bloquear la cuenta por 15 minutos
            }
        }

        // Reiniciar los intentos fallidos y desbloquear la cuenta
        public void ReiniciarIntentos()
        {
            IntentosFallidos = 0;
            BloqueadoHasta = null;
        }
    }
}
