using Application.DTOs.Auth;
using Application.Interfaces;
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
    }
}