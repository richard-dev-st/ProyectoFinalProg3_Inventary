using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Presentation.API.Controllers
{
    [Route("api/inventario")]
    [ApiController]
    public class InventarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // RF-GP-05: acción protegida por el permiso concedido automaticamente al aprobar la solicitud.
        [Authorize(Policy = "Permiso:exportar-inventario")]
        [HttpGet("exportar")]
        public async Task<IActionResult> Exportar()
        {
            var productos = await _context.Productos.AsNoTracking().ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Codigo,Nombre,Precio,StockActual,StockMinimo");
            foreach (var p in productos)
            {
                csv.AppendLine($"{p.Id},{p.Codigo},{p.Nombre},{p.Precio},{p.StockActual},{p.StockMinimo}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "inventario.csv");
        }
    }
}
