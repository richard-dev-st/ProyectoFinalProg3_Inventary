using Application.DTOs.Auditoria;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditoriaController : ControllerBase
    {
        private readonly IAuditoriaService _auditoria;

        public AuditoriaController(IAuditoriaService auditoria)
        {
            _auditoria = auditoria;
        }

        [HttpGet]
        public async Task<IActionResult> Consultar([FromQuery] Guid? usuarioId, [FromQuery] string? entidad, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            try
            {
                var filtro = new FiltroAuditoriaDto(usuarioId, entidad, desde, hasta, pagina, tamanioPagina);

                //Consultamos
                var resultado = await _auditoria.ConsultarAsync(User.IsInRole("Administrador"), filtro, HttpContext.RequestAborted);

                return Ok(resultado);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}