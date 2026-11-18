using Application.DTOs.Correos;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class CorreoColaService : ICorreoColaService
    {
        private readonly IApplicationDbContext _context;

        public CorreoColaService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CorreoEnColaDto>> ObtenerAsync(string? estado = null)
        {
            var consulta = _context.CorreosEnCola.AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                consulta = consulta.Where(c => c.Estado == estado);
            }

            return await consulta
                .OrderByDescending(c => c.FechaCreacion)
                .Select(c => new CorreoEnColaDto
                {
                    Id = c.Id,
                    Destinatario = c.Destinatario,
                    Asunto = c.Asunto,
                    Estado = c.Estado,
                    Intentos = c.Intentos,
                    FechaCreacion = c.FechaCreacion,
                    FechaEnvio = c.FechaEnvio,
                    FechaUltimoIntento = c.FechaUltimoIntento,
                    UltimoError = c.UltimoError
                })
                .ToListAsync();
        }
    }
}
