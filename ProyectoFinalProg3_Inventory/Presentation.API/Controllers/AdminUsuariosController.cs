using Application.Interfaces;
using Core.Application.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.API.Controllers
{
    [ApiController]
    [Route("api/admin/usuarios")]
    [Authorize(Roles = "Administrador")] //RF-CA-05: Administrador puede gestionar usuarios

    public class AdminUsuariosController : ControllerBase
    {
        private readonly IAdminUsuarioService _adminUsuarioService;

        public AdminUsuariosController(IAdminUsuarioService adminUsuarioService)
        {
            _adminUsuarioService = adminUsuarioService;
        }


        /// <summary>
        /// Obtiene la lista de todos los usuarios del sistema.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerUsuarios()
        {
            // Llamamos al servicio para obtener la lista de usuarios
            var usuarios = await _adminUsuarioService.ObtenerUsuariosAsync();
            return Ok(usuarios);
        }

        /// <summary>
        /// Cambia el rol de un usuario (RF-CA-08).
        /// </summary>
        [HttpPut("{id:guid}/rol")]
        public async Task<IActionResult> CambiarRol(Guid id, [FromBody] CambiarRolDto dto)
        {
            //Obtenemos el ID del usuario
            var adminId = ObtenerUsuarioIdActual();

            try
            {
                await _adminUsuarioService.CambiarRolUsuarioAsync(id, dto, adminId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Activar o desactivar cuenta deun usuario (RF-CA-20).
        /// </summary>
        [HttpPut("{id:guid}/estado")]
        public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoDto dto)
        {
            //Obtenemos el Id del usuario
            var adminId = ObtenerUsuarioIdActual();

            try
            {
                await _adminUsuarioService.CambiarEstadoUsuarioAsync(id, dto, adminId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message }); // RF-CA-20 Autodesactivarse a sí mismo
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        private Guid ObtenerUsuarioIdActual()
        {
            //Extraer el Id del usuario desde el claim
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value; // Intentar obtener el claim "sub" si no se encuentra el claim NameIdentifier

            return Guid.Parse(claimId);
        }
    }
}