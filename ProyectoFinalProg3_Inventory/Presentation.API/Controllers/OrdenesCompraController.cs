using System.Security.Claims;
using Application.DTOs.Ordenes;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Presentation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdenesCompraController : ControllerBase
    {
        private readonly IOrdenCompraService _ordenCompraService;
        private readonly IApplicationDbContext _context;

        public OrdenesCompraController(IOrdenCompraService ordenCompraService, IApplicationDbContext context)
        {
            _ordenCompraService = ordenCompraService;
            _context = context;
        }

        [HttpPost("crear-borrador")]
        [Authorize(Roles = "Estandar,Administrador")]
        public async Task<IActionResult> CrearBorrador([FromBody] CrearOrdenCompraDto dto, [FromQuery] Guid usuarioId)
        {
            try
            {
                var ordenId = await _ordenCompraService.CrearBorradorAsync(dto, usuarioId);
                return Ok(new { OrdenId = ordenId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/enviar-aprobacion")]
        [Authorize(Roles = "Estandar,Administrador")]
        public async Task<IActionResult> EnviarAprobacion(int id)
        {
            try
            {
                await _ordenCompraService.EnviarAprobacionAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/aprobar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Aprobar(int id)
        {
            try
            {
                await _ordenCompraService.AprobarAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/recibir")]
        [Authorize(Roles = "Estandar,Administrador")]
        public async Task<IActionResult> Recibir(int id)
        {
            var usuarioId = ObtenerUsuarioIdActual();

            //Validar si el usuario tiene permisos para recibir la orden de compra
            bool tienePermiso = _context.PermisosUsuario.Any(p => p.UsuarioId == usuarioId && p.Permiso == "recibir-orden");
            if(!tienePermiso)
            {
                return StatusCode(403, new { mensaje = "No posees el permiso 'recibir-orden'. Debes solicitarlo a través de gestión de permisos." });
            }

            try
            {
                await _ordenCompraService.RecibirAsync(id, ObtenerUsuarioIdActual());
                return Ok();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/cancelar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                await _ordenCompraService.CancelarAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/total-en-dop")]
        [Authorize(Roles = "Estandar,Administrador")]
        public async Task<IActionResult> ObtenerTotalEnDop(int id, CancellationToken ct)
        {
            try
            {
                var totalEnDop = await _ordenCompraService.ObtenerTotalEnDopAsync(id, ct);
                return Ok(totalEnDop);
            }
            catch (ServicioExternoException ex)
            {
                var status = ex.Motivo switch
                {
                    ServicioExternoException.Causa.Timeout => StatusCodes.Status504GatewayTimeout,
                    ServicioExternoException.Causa.RespuestaInvalida => StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status503ServiceUnavailable
                };

                var message = ex.Motivo == ServicioExternoException.Causa.Timeout
                    ? "La API de tasas de cambio no respondió a tiempo."
                    : "No se pudo obtener la tasa de cambio. Inténtelo más tarde.";

                return StatusCode(status, new { message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Ocurrió un error inesperado al convertir el total." });
            }
        }

        private Guid ObtenerUsuarioIdActual()
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            return Guid.Parse(claimId);
        }
    }
}
