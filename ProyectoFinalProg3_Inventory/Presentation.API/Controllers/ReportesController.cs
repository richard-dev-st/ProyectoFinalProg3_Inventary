using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.API.Controllers
{
    /// <summary>
    /// RF-REP-01..05: reportes con agregación. El rol y la identidad salen del token.
    /// </summary>
    [Route("api/reportes")]
    [ApiController]
    [Authorize]
    public class ReportesController : ControllerBase
    {
        private readonly IReporteService _reporteService;

        public ReportesController(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [HttpGet("solicitudes")]
        public async Task<IActionResult> Solicitudes(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.ObtenerSolicitudesAsync(
                    ObtenerUsuarioIdActual(),
                    User.IsInRole("Administrador"),
                    desde,
                    hasta);

                return Ok(reporte);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("ordenes")]
        public async Task<IActionResult> Ordenes(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.ObtenerOrdenesAsync(
                    ObtenerUsuarioIdActual(),
                    User.IsInRole("Administrador"),
                    desde,
                    hasta);

                return Ok(reporte);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private Guid ObtenerUsuarioIdActual()
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            return Guid.Parse(claimId!);
        }
    }
}
