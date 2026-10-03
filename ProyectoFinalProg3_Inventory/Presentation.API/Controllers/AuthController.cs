using Application.DTOs.Auth;
using Application.DTOs.Login;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistroRequestDto request)
        {
            //Esto se encarga de obtener la url base del proyecto para poder enviarla en el correo de activacion
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            //Se crea un nuevo objeto request con la url base incluida
            var requestConBaseUrl = request with { BaseUrl = baseUrl };

            var resultado = await _authService.RegistrarAsync(requestConBaseUrl);

            if (!resultado.Exito)
            {
                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        [HttpGet("activar")]
        public async Task<IActionResult> Activar([FromQuery] string token)
        {
            var resultado = await _authService.ActivarCuentaAsync(token);

            if (!resultado.Exito)
            {
                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        [HttpPost("reenviar-activacion")]
        public async Task<IActionResult> ReenviarActivacion([FromQuery] string email)
        {
            //Esto se encarga de obtener la url base del proyecto para poder enviarla en el correo de activacion
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var resultado = await _authService.ReenviarActivacionAsync(email, baseUrl);

            if (!resultado.Exito)
            {
                return BadRequest(resultado);
            }

            return Ok(resultado);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var resultado = await _authService.LoginAsync(request);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> ObtenerUsuarioAutenticado()
        {
            //Extraer el Id del usuario desde el claim del token JWT
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            //Verificar si el claim es nulo o no es un GUID válido
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var usuarioId))
            {
                return Unauthorized(new { mensaje = "Sin sesión válida." });
            }

            //Llamar al servicio para obtener los datos del usuario autenticado
            var usuario = await _authService.ObtenerUsuarioAutenticadoAsync(usuarioId);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Sin sesión válida." });
            }

            return Ok(usuario);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            //Extraer el token JWT de la cabecera Authorization
            string? token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            //Declarar una variable para almacenar el resultado del logout
            var resultado = await _authService.LogoutAsync(token);

            if (!resultado.Exito)
            {
                return BadRequest(resultado);
            }
            //Retornamos ok con el resultado.
            return Ok(resultado);
        }

        [HttpPost("solicitar-recuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitarRecuperacionDto dto)
        {
            await _authService.SolicitarRecuperacionPasswordAsync(dto);

            //Respuesta exitosa, no se devuelve información sensible
            return Ok(new { mensaje = "Si el correo esta registrado y activo, se ha enviado un enlace de recuperación." });
        }

        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto)
        {
            try
            {
                await _authService.RestablecerPasswordAsync(dto);
                return Ok(new { mensaje = "Contraseña restablecida exitosamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            //Extraer el Id del usuario desde el claim del token JWT
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            //Verificar si el claim es nulo o no es un GUID válido
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var usuarioId))
            {
                return Unauthorized(new { mensaje = "Sin sesión válida." });
            }

            try
            {
                await _authService.CambiarPasswordAsync(usuarioId, dto);
                return Ok(new { mensaje = "Contraseña actualizada exitosamente." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpPost("{id:guid}/forzar-restablecimiento")]
        public async Task<IActionResult> ForzarRestablecimiento(Guid id, [FromServices] IAuthService authService)
        {
            try
            {
                await authService.ForzarRestablecimientoPasswordAsync(id);
                return Ok(new { mensaje = "Se ha forzado el restablecimiento y enviado el código por correo." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }
    }
}