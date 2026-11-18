using Application.DTOs.Notificaciones;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.API.Controllers
{
    /// <summary>
    /// RF-NOT-04/05/07: notificaciones propias y preferencia del canal de correo.
    /// Toda operación exige sesión válida (RD-06).
    /// </summary>
    [Route("api/notificaciones")]
    [ApiController]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _notificaciones;

        public NotificacionesController(INotificacionService notificaciones)
        {
            _notificaciones = notificaciones;
        }

        // RF-NOT-04: solo las notificaciones del usuario autenticado.
        [HttpGet]
        public async Task<IActionResult> ObtenerPropias([FromQuery] bool soloNoLeidas = false)
        {
            var notificaciones = await _notificaciones.ObtenerPropiasAsync(
                ObtenerUsuarioIdActual(),
                soloNoLeidas);

            return Ok(notificaciones);
        }

        // RF-NOT-05: marcar una notificación propia como leída.
        [HttpPost("{id:guid}/leer")]
        public async Task<IActionResult> MarcarLeida(Guid id)
        {
            var marcada = await _notificaciones.MarcarLeidaAsync(id, ObtenerUsuarioIdActual());

            // 404 también cuando pertenece a otro usuario: no se filtra su existencia.
            return marcada ? NoContent() : NotFound();
        }

        // RF-NOT-07: consultar la preferencia actual.
        [HttpGet("preferencia")]
        public async Task<IActionResult> ObtenerPreferencia()
        {
            var recibeCorreos = await _notificaciones.ObtenerPreferenciaCorreoAsync(ObtenerUsuarioIdActual());

            return Ok(new PreferenciaCorreoDto { RecibeCorreos = recibeCorreos });
        }

        // RF-NOT-07: activar o desactivar el canal de correo sin perder el interno.
        [HttpPut("preferencia")]
        public async Task<IActionResult> ActualizarPreferencia([FromBody] PreferenciaCorreoDto dto)
        {
            try
            {
                await _notificaciones.ActualizarPreferenciaCorreoAsync(
                    ObtenerUsuarioIdActual(),
                    dto.RecibeCorreos);

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private Guid ObtenerUsuarioIdActual()
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            return Guid.Parse(claimId!);
        }
    }
}
