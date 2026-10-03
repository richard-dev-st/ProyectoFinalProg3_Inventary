using Application.Interfaces;
using Core.Domain.Entities;
using System;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Text;
using Application.DTOs.Auth;
using Application.DTOs.Login;
using Core.Application.Interfaces;
using Core.Application.Configurations;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IApplicationDbContext context, IPasswordService passwordService, IJwtService jwtService, JwtSettings jwtSettings)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings;
        }

        public async Task<AuthResponseDto> ActivarCuentaAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthResponseDto(false, "El token de activación es requerido.");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.TokenActivacion == token);

            if (usuario == null)
            {
                return new AuthResponseDto(false, "El token de activación no es válido o no existe.");
            }

            //Llamamos al método ActivarCuenta del usuario para cambiar su estado a activado
            bool activado = usuario.ActivarCuenta(token);

            if (!activado)
            {
                return new AuthResponseDto(false, "No se pudo activar la cuenta. El token puede estar expirado o la cuenta ya está activada.");
            }

            await _context.SaveChangesAsync();

            return new AuthResponseDto(true, "Cuenta activada con éxito. Ya puedes iniciar sesión.");
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            //Mensaje generico de seguridad si no existe el usuario (RF-CA-03)
            if (usuario == null)
            {
                throw new InvalidOperationException("Correo o contrasena incorrectos");
            }

            //Verificamos si la cuenta está activa
            if (!usuario.Activo)
            {
                throw new InvalidOperationException("La cuenta no está activa. Por favor revisa tu correo.");
            }

            //Verificar si esta bloqueado por 15 minutos tras realizar los 5 intentos fallidos (RF-CA-19)
            if (usuario.EstaBloqueado())
            {
                throw new InvalidOperationException("La cuenta está temporalmente bloqueada por demasiados intentos fallidos. Intenta más tarde.");
            }

            //validar contrasena
            bool passwordValida = _passwordService.VerifyPassword(request.Password, usuario.PasswordHash);

            if (!passwordValida)
            {
                usuario.RegistrarIntentoFallido();
                await _context.SaveChangesAsync();

                throw new InvalidOperationException("Correo o contrasena incorrectos");
            }

            //Credenciales validas: reiniciar contador de intentos fallidos (RF-CA-19)
            usuario.ReiniciarIntentos();
            await _context.SaveChangesAsync();

            //Generar JWT (Implementamos el generador de token)
            string token = _jwtService.GenerarToken(usuario);

            return new LoginResponseDto(
                Token: token,
                Email: usuario.Email,
                Rol: usuario.Rol.ToString(),
                Expiracion: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationInHours)
                );
        }

        public async Task<AuthResponseDto> LogoutAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthResponseDto(false, "El token provisto no es válido.");
            }

            //Limpiar el prefijo "Bearer " si está presente
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7).Trim();
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return new AuthResponseDto(false, "Formato de token inválido.");
            }

            var jwtToken = handler.ReadJwtToken(token);
            var jti = jwtToken.Id
                      ?? jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti || c.Type == "jti")?.Value; //Obtenemos el JTI del token

            if (string.IsNullOrEmpty(jti))
            {
                return new AuthResponseDto(false, "El token no contiene un identificador único (JTI).");
            }

            var expiracion = jwtToken.ValidTo; //Obtenemos la fecha de expiración del token

            //Extraemos el UsuarioId del claim "sub" (subject) del token
            var usuarioIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            //Intentamos convertir el claim a Guid, si falla, asignamos null
            Guid? usuarioId = Guid.TryParse(usuarioIdClaim, out var parseGuid) ? parseGuid : null;

            //Verificamos si el token ya ha sido revocado
            var yaRevocado = await _context.TokensRevocados.AnyAsync(t => t.TokenJti == jti);

            if (!yaRevocado)
            {
                _context.TokensRevocados.Add(new TokenRevocado
                {
                    TokenJti = jti,
                    UsuarioId = usuarioId.GetValueOrDefault(),
                    FechaRevocacion = DateTime.UtcNow,
                    FechaExpiracion = expiracion
                });
                await _context.SaveChangesAsync();
            }

            return new AuthResponseDto(true, "Sesión cerrada exitosamente.");
        }

        public async Task<UsuarioSesionDto?> ObtenerUsuarioAutenticadoAsync(Guid usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null || !usuario.Activo)
            {
                return null;
            }

            return new UsuarioSesionDto(
                Id: usuario.Id,
                Email: usuario.Email,
                Rol: usuario.Rol.ToString(),
                Activo: usuario.Activo
            );
        }

        public async Task<AuthResponseDto> ReenviarActivacionAsync(string email, string baseUrl)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            //Si el usuario existe y no está activo, generamos un nuevo token de activación y lo enviamos por correo
            if (usuario != null && !usuario.Activo)
            {
                //Generamos un nuevo token de activación y lo asignamos al usuario
                usuario.GenerarTokenActivacion();

                string enlaceActivacion = $"{baseUrl}/api/auth/activar?token={usuario.TokenActivacion}";
                string cuerpoCorreo = $"<p>Hola,</p><p>Has solicitado un nuevo enlace de activación:</p><p><a href='{enlaceActivacion}'>Activar Cuenta</a>";

                _context.CorreosEnCola.Add(new CorreoEnCola(usuario.Email, "Nuevo enlace de activación", cuerpoCorreo));

                await _context.SaveChangesAsync();
            }

            return new AuthResponseDto(true, "Si el correo está registrado y no ha sido activado, se ha enviado un nuevo enlace de activación.");
        }

        public async Task<AuthResponseDto> RegistrarAsync(RegistroRequestDto request)
        {
            //Validar politica de contrasena (RF-CA-14)
            if(!_passwordService.ValidarPolitica(request.Password, out var mensajePolitica))
            {
                return new AuthResponseDto(false, mensajePolitica);
            }

            // Verificamos si el usuario ya existe
            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (existeUsuario)
            {
                return new AuthResponseDto(false, "El correo electrónico ya está registrado.");
            }

            string passwordHash = _passwordService.HashPassword(request.Password);

            var nuevoUsuario = new Usuario(request.Email, passwordHash);

            _context.Usuarios.Add(nuevoUsuario);

            string enlaceActivacion = $"{request.BaseUrl}/api/auth/activar?token={nuevoUsuario.TokenActivacion}";
            string cuerpoCorreo = $"<p>Hola,</p><p>Para activar tu cuenta, haz clic en el siguiente enlace:</p><p><a href='{enlaceActivacion}'>Activar Cuenta</a>";

            var correo = new CorreoEnCola(nuevoUsuario.Email, "Confirmación de cuenta", cuerpoCorreo);
            _context.CorreosEnCola.Add(correo);

            await _context.SaveChangesAsync();

            return new AuthResponseDto(true, "Registro exitoso. Se ha enviado un correo para activar tu cuenta.");

        }

        public async Task CambiarPasswordAsync(Guid usuarioId, CambiarPasswordDto dto)
        {
            //Primero obtenemos el usuario de la base de datos
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            //Si el usuario no existe, lanzamos una excepción
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado.");
            }

            //Verificamos si la contraseña actual proporcionada coincide con la almacenada en la base de datos
            var verificacion = _passwordService.VerifyPassword(dto.PasswordActual, usuario.PasswordHash);

           
            if (!verificacion)
            {
                throw new InvalidOperationException("La contraseña actual es incorrecta.");
            }

            //Validar politica de contrasena (RF-CA-14)
            if (!_passwordService.ValidarPolitica(dto.NuevaPassword, out var mensajePolitica))
            {
                throw new InvalidOperationException(mensajePolitica);
            }

            var nuevoHash = _passwordService.HashPassword(dto.NuevaPassword);
            usuario.EstablecerNuevaPassword(nuevoHash);

            await _context.SaveChangesAsync();
        }

        public async Task ForzarRestablecimientoPasswordAsync(Guid usuarioId)
        {
            //Buscamos el usuario en la base de datos
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            //Validamos si el usuario existe o no
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado.");
            }

            usuario.EstablecerNuevaPassword("RESET_FORZADO_" + Guid.NewGuid().ToString("N")); //Invalida clave vieja y sesiones (limpia el código previo)
            usuario.GenerarCodigoRecuperacion();

            string cuerpoCorreo = $"<p>Un administrador ha iniciado el restablecimiento de tu contraseña. Tu código es: <strong>{usuario.CodigoRecuperacion}</strong>.</p>";

            var correoEnCola = new CorreoEnCola(usuario.Email, "Restablecimiento de contraseña solicitado por un administrador", cuerpoCorreo);

            _context.CorreosEnCola.Add(correoEnCola);
            await _context.SaveChangesAsync();

        }

        // RF-CA-10, RF-CA-11 y RF-CA-12: Restablecimiento público con código
        public async Task RestablecerPasswordAsync(RestablecerPasswordDto dto)
        {
            var emailLimpio = dto.Email.ToLowerInvariant().Trim();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailLimpio);

            if (usuario == null)
            {
                throw new KeyNotFoundException("El código es inválido o ha expirado.");
            }

            //Validar que el codigo coincida y no este vencido
            if (string.IsNullOrEmpty(usuario.CodigoRecuperacion) || usuario.CodigoRecuperacion != dto.Codigo || usuario.CodigoRecuperacionExpiracion == null || DateTime.UtcNow > usuario.CodigoRecuperacionExpiracion)
            {
                throw new InvalidOperationException("El código es inválido o ha expirado.");
            }

            // Validar política para la nueva contraseña (RF-CA-14)
            if (!_passwordService.ValidarPolitica(dto.NuevaPassword, out var mensajePolitica))
            {
                throw new InvalidOperationException(mensajePolitica);
            }

            var nuevoHash = _passwordService.HashPassword(dto.NuevaPassword);
            usuario.EstablecerNuevaPassword(nuevoHash);

            await _context.SaveChangesAsync();
        }

        //RF-CA-09: La respuesta no revela que correos estan registrados
        public async Task SolicitarRecuperacionPasswordAsync(SolicitarRecuperacionDto dto)
        {
            //Hacemos que el email sea insensible a mayúsculas y minúsculas y eliminamos espacios en blanco al inicio y al final
            var emailLimpio = dto.Email.ToLowerInvariant().Trim();

            //buscamos el usuario por email
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailLimpio);

            //Si el usuario no existe o no está activo, simplemente retornamos sin hacer nada
            if (usuario == null || !usuario.Activo)
            {
                return;
            }

            //Generar el codigo de la entidad
            usuario.GenerarCodigoRecuperacion();

            string cuerpoCorreo = $"<p>Tu código de recuperación es: <strong>{usuario.CodigoRecuperacion}</strong></p><p>Este código expirará en 15 minutos.</p>";

            //Creamos la entrada para el envio de correo
            var correoEnCola = new CorreoEnCola(usuario.Email, "Código de recuperación de contraseña", cuerpoCorreo);

            //Agregamos el correo a la cola y guardamos los cambios
            _context.CorreosEnCola.Add(correoEnCola);
            await _context.SaveChangesAsync();
        }
    }
}
