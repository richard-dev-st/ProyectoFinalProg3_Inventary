using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers
{
    /// <summary>
    /// RF-NOT-11: consulta de la cola de correos. Solo un Administrador puede verla.
    /// </summary>
    [Route("api/correos")]
    [ApiController]
    [Authorize(Roles = "Administrador")]
    public class CorreosController : ControllerBase
    {
        private readonly ICorreoColaService _correoColaService;

        public CorreosController(ICorreoColaService correoColaService)
        {
            _correoColaService = correoColaService;
        }

        [HttpGet]
        public async Task<IActionResult> Obtener([FromQuery] string? estado = null)
        {
            var correos = await _correoColaService.ObtenerAsync(estado);

            return Ok(correos);
        }
    }
}
