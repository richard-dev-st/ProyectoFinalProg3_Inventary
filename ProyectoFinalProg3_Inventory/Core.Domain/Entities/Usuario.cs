using Core.Domain.Entities.Enums;

namespace Core.Domain.Entities
{
    public class Usuario
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public RolUsuario Rol { get; private set; }
        public bool Activo { get; private set; } = false;

        //Tokens de Activacion
        public string? TokenActivacion { get; private set; }
        public DateTime? TokenActivacionExpiracion { get; private set; }

        //Intentos de inicio de sesión fallidos
        public int IntentosFallidos { get; private set; } = 0;

        //Bloqueo de cuenta
        public DateTime? BloqueadoHasta { get; private set; }

        //Gestionar el codigo de recuperacion de contraseña
        public string? CodigoRecuperacion { get; private set; }
        public DateTime? CodigoRecuperacionExpiracion { get; private set; }
        public DateTime? FechaUltimoCambioPassword { get; private set; } // Muy util para RF-CA-12

        private Usuario() { } // Constructor privado para EF Core)

        public Usuario(string email, string passwordHash)
        {
            Id = Guid.NewGuid();
            Email = email.ToLowerInvariant().Trim();
            PasswordHash = passwordHash;
            Rol = RolUsuario.Estandar;
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

        //Cambiar el rol del usuario (RF-CA-08)
        public void CambiarRol(RolUsuario nuevoRol, Guid adminIdQueRealizaCambio)
        {
            //Un administrador no puede cambiar su propio rol si eso compromete la seguridad del sistema
            Rol = nuevoRol;
        }

        //Desactivar usuario con validacion de no desactivarse a si mismo (RF-CA-20)
        public void Desactivar(Guid adminIdQueRealizaCambio)
        {
            //Un administrador no puede desactivarse a si mismo
            if (Id == adminIdQueRealizaCambio)
            {
                throw new InvalidOperationException("Un administrador no puede desactivarse a sí mismo.");
            }
            //Desactivar usuario
            Activo = false;
        }

        //Reactivar usuario (RF-CA-20)
        public void Reactivar()
        {
            Activo = true;
        }

        //Generar codigo de 6 digitos para recuperacion de contraseña
        public void GenerarCodigoRecuperacion()
        {
            CodigoRecuperacion = new Random().Next(100000, 999999).ToString(); // Genera un codigo de 6 dígitos
            CodigoRecuperacionExpiracion = DateTime.UtcNow.AddMinutes(15); // Codigo válido por 15 minutos
        }

        //Metodo para completar restablecimiento de contrasena (RF-CA-11)
        public void EstablecerNuevaPassword(string nuevoHash)
        {
            PasswordHash = nuevoHash;
            CodigoRecuperacion = null; // Limpiar el código de recuperación
            CodigoRecuperacionExpiracion = null; // Limpiar la expiración del código
            FechaUltimoCambioPassword = DateTime.UtcNow; // Actualizar la fecha del último
        }
    }
}
