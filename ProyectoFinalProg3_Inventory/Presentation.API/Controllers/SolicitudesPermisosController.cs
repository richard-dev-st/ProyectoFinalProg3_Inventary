using Application.DTOs.Permisos;
using Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.API.Controllers
{
    [Route("api/solicitudes")]
    [ApiController]
    public class SolicitudesPermisosController : ControllerBase
    {
        private readonly ISolicitudPermisoService _solicitudPermisoService;

        public SolicitudesPermisosController(ISolicitudPermisoService solicitudPermisoService)
        {
            _solicitudPermisoService = solicitudPermisoService;
        }

        // RF-GP-01: un usuario autenticado crea una solicitud (nace en Pendiente).
        // No se restringe a Estándar porque RF-GP-07 exige que un Administrador también pueda crear la suya.
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CrearSolicitud([FromBody] CrearSolicitudDto dto)
        {
            var usuarioId = ObtenerUsuarioIdActual();

            try
            {
                var solicitud = await _solicitudPermisoService.CrearAsync(usuarioId, dto.Permiso, dto.Descripcion);
                return Ok(Mapear(solicitud));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // RF-GP-02: solo un Administrador consulta las solicitudes pendientes.
        [Authorize(Roles = "Administrador")]
        [HttpGet("pendientes")]
        public async Task<IActionResult> ObtenerSolicitudesPendientes()
        {
            var solicitudes = await _solicitudPermisoService.PendientesAsync();
            return Ok(solicitudes.Select(Mapear));
        }

        // RF-GP-02: un usuario solo ve sus propias solicitudes (el id sale del token, no del cliente).
        [Authorize]
        [HttpGet("mias")]
        public async Task<IActionResult> ObtenerMisSolicitudes()
        {
            var usuarioId = ObtenerUsuarioIdActual();
            var solicitudes = await _solicitudPermisoService.MiasAsync(usuarioId);
            return Ok(solicitudes.Select(Mapear));
        }

        // RF-GP-03, RF-GP-05, RF-GP-06, RF-GP-07.
        [Authorize(Roles = "Administrador")]
        [HttpPost("{id:guid}/aprobar")]
        public async Task<IActionResult> AprobarSolicitud(Guid id)
        {
            var usuarioId = ObtenerUsuarioIdActual();

            try
            {
                var solicitud = await _solicitudPermisoService.AprobarAsync(id, usuarioId);
                if (solicitud == null)
                {
                    return NotFound(new { mensaje = "Solicitud no encontrada." });
                }
                return Ok(Mapear(solicitud));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // RF-GP-04, RF-GP-06, RF-GP-07.
        [Authorize(Roles = "Administrador")]
        [HttpPost("{id:guid}/rechazar")]
        public async Task<IActionResult> RechazarSolicitud(Guid id, [FromBody] RechazarSolicitudDto dto)
        {
            var usuarioId = ObtenerUsuarioIdActual();

            try
            {
                var solicitud = await _solicitudPermisoService.RechazarAsync(id, usuarioId, dto.Motivo);
                if (solicitud == null)
                {
                    return NotFound(new { mensaje = "Solicitud no encontrada." });
                }
                return Ok(Mapear(solicitud));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // RF-GP-05: el solicitante consulta los permisos que ya le fueron concedidos.
        [Authorize]
        [HttpGet("/api/mis-permisos")]
        public async Task<IActionResult> ObtenerMisPermisos()
        {
            var usuarioId = ObtenerUsuarioIdActual();
            var permisos = await _solicitudPermisoService.MisPermisosAsync(usuarioId);
            return Ok(permisos);
        }

        private Guid ObtenerUsuarioIdActual()
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return Guid.Parse(claimId);
        }

        private static SolicitudPermisoDto Mapear(SolicitudPermiso s) => new()
        {
            Id = s.Id,
            SolicitanteUsuarioId = s.SolicitanteUsuarioId,
            Permiso = s.Permiso,
            Descripcion = s.Descripcion,
            Estado = s.Estado.ToString(),
            CreadoEn = s.CreadoEn,
            ResueltoPorUsuarioId = s.ResueltoPorUsuarioId,
            FechaResolucion = s.FechaResolucion,
            MotivoRechazo = s.MotivoRechazo
        };
    }
}
