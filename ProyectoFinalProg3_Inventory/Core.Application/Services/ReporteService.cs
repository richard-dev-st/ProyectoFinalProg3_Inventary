using Application.DTOs.Reportes;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ReporteService : IReporteService
    {
        private readonly IApplicationDbContext _context;

        public ReporteService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReporteSolicitudesDto> ObtenerSolicitudesAsync(
            Guid usuarioId,
            bool esAdministrador,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            ValidarRango(desde, hasta);

            var consulta = _context.SolicitudesPermiso.AsQueryable();

            // RF-REP-04: un Estándar no obtiene datos de otros usuarios.
            if (!esAdministrador)
            {
                consulta = consulta.Where(s => s.SolicitanteUsuarioId == usuarioId);
            }

            if (desde.HasValue)
            {
                consulta = consulta.Where(s => s.CreadoEn >= desde.Value);
            }

            if (hasta.HasValue)
            {
                consulta = consulta.Where(s => s.CreadoEn <= hasta.Value);
            }

            // RF-REP-03: la agregación se hace en el motor de datos.
            var grupos = await consulta
                .GroupBy(s => s.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            var resultado = grupos
                .Select(g => new GrupoSolicitudesDto
                {
                    Estado = g.Estado.ToString(),
                    Cantidad = g.Cantidad
                })
                .OrderBy(g => g.Estado)
                .ToList();

            return new ReporteSolicitudesDto
            {
                Desde = desde,
                Hasta = hasta,
                Total = resultado.Sum(g => g.Cantidad),
                Grupos = resultado
            };
        }

        public async Task<ReporteOrdenesDto> ObtenerOrdenesAsync(
            Guid usuarioId,
            bool esAdministrador,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            ValidarRango(desde, hasta);

            var consulta = _context.OrdenesCompra.AsQueryable();

            // RF-REP-04: un Estándar solo ve las órdenes que creó.
            if (!esAdministrador)
            {
                consulta = consulta.Where(o => o.CreadoPorUsuarioId == usuarioId);
            }

            if (desde.HasValue)
            {
                consulta = consulta.Where(o => o.FechaCreacion >= desde.Value);
            }

            if (hasta.HasValue)
            {
                consulta = consulta.Where(o => o.FechaCreacion <= hasta.Value);
            }

            // RF-REP-03: GROUP BY + COUNT/SUM en el motor de datos.
            var grupos = await consulta
                .GroupBy(o => new { o.ProveedorId, o.Estado })
                .Select(g => new
                {
                    g.Key.ProveedorId,
                    g.Key.Estado,
                    Cantidad = g.Count(),
                    Monto = g.Sum(o => o.Total)
                })
                .ToListAsync();

            var proveedores = await _context.Proveedores
                .ToDictionaryAsync(p => p.Id, p => p.Nombre);

            var resultado = grupos
                .Select(g => new GrupoOrdenesDto
                {
                    ProveedorId = g.ProveedorId,
                    Proveedor = proveedores.TryGetValue(g.ProveedorId, out var nombre)
                        ? nombre
                        : $"Proveedor {g.ProveedorId}",
                    Estado = g.Estado.ToString(),
                    CantidadOrdenes = g.Cantidad,
                    MontoTotalUsd = g.Monto
                })
                .OrderBy(g => g.Proveedor)
                .ThenBy(g => g.Estado)
                .ToList();

            return new ReporteOrdenesDto
            {
                Desde = desde,
                Hasta = hasta,
                TotalOrdenes = resultado.Sum(g => g.CantidadOrdenes),
                MontoTotalUsd = resultado.Sum(g => g.MontoTotalUsd),
                Grupos = resultado
            };
        }

        private static void ValidarRango(DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue && hasta.Value < desde.Value)
            {
                throw new InvalidOperationException(
                    "El rango de fechas es inválido: 'hasta' no puede ser anterior a 'desde'.");
            }
        }
    }
}
