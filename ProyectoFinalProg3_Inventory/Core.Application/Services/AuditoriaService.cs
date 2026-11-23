using Application.DTOs.Auditoria;
using Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private const int TamanoPaginaMaximo = 100;

        private readonly IApplicationDbContext _context;

        public AuditoriaService(IApplicationDbContext context)
        {
            _context = context;
        }

        // RF-AUD-01: el registro se crea con todos sus campos obligatorios validados en el dominio.
        public RegistroAuditoria Preparar(Guid usuarioId, string usuarioEmail, string accion,
            string entidad, string entidadId,
            string? valorAnterior = null, string? valorNuevo = null,
            DateTime? fechaHoraUtc = null)
            => RegistroAuditoria.Crear(usuarioId, usuarioEmail, accion, entidad, entidadId,
                fechaHoraUtc ?? DateTime.UtcNow, valorAnterior, valorNuevo);

        // RF-AUD-07: consulta filtrable por usuario, entidad y rango de fechas, solo Administrador.
        public async Task<ResultadoAuditoriaDto> ConsultarAsync(
            bool esAdministrador, FiltroAuditoriaDto filtro, CancellationToken ct = default)
        {
            // RD-06: se rechaza aunque se invoque directamente al servicio.
            if (!esAdministrador)
            {
                throw new UnauthorizedAccessException(
                    "Solo un Administrador puede consultar el historial de auditoría.");
            }

            // RD-07: un filtro inválido produce un rechazo controlado.
            ValidarFiltro(filtro);

            IQueryable<RegistroAuditoria> consulta = _context.Auditoria.AsNoTracking();

            // RF-AUD-07: filtro por usuario.
            if (filtro.UsuarioId.HasValue)
            {
                var usuarioId = filtro.UsuarioId.Value;
                consulta = consulta.Where(r => r.UsuarioId == usuarioId);
            }

            // RF-AUD-07: filtro por entidad (coincidencia exacta con el vocabulario usado al registrar).
            if (!string.IsNullOrWhiteSpace(filtro.Entidad))
            {
                var entidad = filtro.Entidad.Trim();
                consulta = consulta.Where(r => r.Entidad == entidad);
            }

            // RF-AUD-07: rango de fechas inclusivo.
            if (filtro.Desde.HasValue)
            {
                var desde = filtro.Desde.Value;
                consulta = consulta.Where(r => r.FechaHoraUtc >= desde);
            }

            if (filtro.Hasta.HasValue)
            {
                var hasta = filtro.Hasta.Value;

                // Una fecha sin hora incluye el día completo; una marca con hora respeta el instante.
                if (hasta.TimeOfDay == TimeSpan.Zero)
                {
                    // Hallazgo 1 de la Asignación 3: el último día representable no admite AddDays(1).
                    if (hasta.Date < DateTime.MaxValue.Date)
                    {
                        var limiteExclusivo = hasta.AddDays(1);
                        consulta = consulta.Where(r => r.FechaHoraUtc < limiteExclusivo);
                    }
                }
                else
                {
                    consulta = consulta.Where(r => r.FechaHoraUtc <= hasta);
                }
            }

            var total = await consulta.CountAsync(ct);

            var registros = await consulta
                .OrderByDescending(r => r.FechaHoraUtc)
                .ThenByDescending(r => r.Id)
                .Skip((filtro.Pagina - 1) * filtro.TamanioPagina)
                .Take(filtro.TamanioPagina)
                .Select(r => new RegistroAuditoriaDto(
                    r.Id,
                    r.UsuarioId,
                    r.UsuarioEmail,
                    r.Accion,
                    r.Entidad,
                    r.EntidadId,
                    r.FechaHoraUtc,
                    r.ValorAnterior,
                    r.ValorNuevo))
                .ToListAsync(ct);

            return new ResultadoAuditoriaDto(registros, total, filtro.Pagina, filtro.TamanioPagina);
        }

        private static void ValidarFiltro(FiltroAuditoriaDto filtro)
        {
            if (filtro.Pagina < 1)
            {
                throw new InvalidOperationException("La página debe ser mayor o igual a 1.");
            }

            if (filtro.TamanioPagina < 1 || filtro.TamanioPagina > TamanoPaginaMaximo)
            {
                throw new InvalidOperationException(
                    $"El tamaño de página debe estar entre 1 y {TamanoPaginaMaximo}.");
            }

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Hasta.Value < filtro.Desde.Value)
            {
                throw new InvalidOperationException(
                    "El rango de fechas es inválido: 'hasta' no puede ser anterior a 'desde'.");
            }
        }
    }
}
