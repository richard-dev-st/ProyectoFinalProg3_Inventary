using Application.DTOs.Documentos;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentosController : ControllerBase
    {

        private readonly IDocumentoService _service;

        public DocumentosController(IDocumentoService service)
        {
            _service = service;
        }

        //POST /api/documentos/subir
        [Authorize]
        [HttpPost("/api/documentos/subir")]
        public async Task<IActionResult> Subir([FromForm] IFormFile archivo)
        {
            try
            {
                if (archivo is null || archivo.Length == 0)
                {
                    return BadRequest(new { mensaje = "Debes enviar un archivo." });
                }

                await using var stream = archivo.OpenReadStream();
                var entrada = new ArchivoEntrada(archivo.FileName, archivo.ContentType, archivo.Length, stream);
                var dto = await _service.SubirDocumentoAsync(entrada, ObtenerUsuarioIdActual());
                return Ok(dto);
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

        //GET /api/documentos
        [Authorize]
        [HttpGet("/api/documentos")]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var usuarioId = ObtenerUsuarioIdActual();
                var documentos = await _service.ListarDocumentosAsync(usuarioId);
                return Ok(documentos);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { mensaje = ex.Message });
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
        //GET /api/documentos/{id}/descargar
        [Authorize]
        [HttpGet("/api/documentos/{id}/descargar")]
        public async Task<IActionResult> Descargar(Guid id)
        {
            try
            {
                var usuarioId = ObtenerUsuarioIdActual();
                var documento = await _service.DescargarDocumentoAsync(id, usuarioId);

                //ASP.NET escrible el stream en la respuesta y lo dispone al terminar. 
                var tipoContenido = string.IsNullOrWhiteSpace(documento.TipoContenido) ? "application/octet-stream" : documento.TipoContenido;

                return File(documento.Contenido, tipoContenido, documento.NombreOriginal);
            }
            catch(UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = ex.Message });
            }
            catch(InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (IOException)
            {
                return NotFound(new { mensaje = "El archivo ya no está disponible." });
            }
        }

        //DELETE /api/documentos/{id}
        [Authorize]
        [HttpDelete("/api/documentos/{id}")]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            try
            {
                var usuarioId = ObtenerUsuarioIdActual();
                await _service.EliminarDocumentoAsync(id, usuarioId);
                return Ok();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        private Guid ObtenerUsuarioIdActual()
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(claimId, out var usuarioId))
            {
                throw new UnauthorizedAccessException("Sesión no válida.");
            }
            return usuarioId;
        }
    }
}
