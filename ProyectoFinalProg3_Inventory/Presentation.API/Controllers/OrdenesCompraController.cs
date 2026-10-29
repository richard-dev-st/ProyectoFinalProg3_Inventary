using System.Security.Claims;
using Application.DTOs.Ordenes;
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

        private Guid ObtenerUsuarioIdActual()
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            return Guid.Parse(claimId);
        }
    }
}
