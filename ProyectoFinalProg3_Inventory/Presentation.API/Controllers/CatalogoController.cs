using Application.DTOs.Catalogo;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Controllers
{
    // Datos maestros del módulo de negocio. Lectura para cualquier usuario con sesión
    // (el formulario de órdenes los necesita); alta solo para Administrador.
    [Route("api/catalogo")]
    [ApiController]
    [Authorize]
    public class CatalogoController : ControllerBase
    {
        private readonly ICatalogoService _catalogo;

        public CatalogoController(ICatalogoService catalogo)
        {
            _catalogo = catalogo;
        }

        [HttpGet("proveedores")]
        public async Task<IActionResult> ListarProveedores()
        {
            return Ok(await _catalogo.ListarProveedoresAsync());
        }

        [HttpPost("proveedores")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearProveedor([FromBody] CrearProveedorDto dto)
        {
            try
            {
                return Ok(await _catalogo.CrearProveedorAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("productos")]
        public async Task<IActionResult> ListarProductos()
        {
            return Ok(await _catalogo.ListarProductosAsync());
        }

        [HttpPost("productos")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearProducto([FromBody] CrearProductoDto dto)
        {
            try
            {
                return Ok(await _catalogo.CrearProductoAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
