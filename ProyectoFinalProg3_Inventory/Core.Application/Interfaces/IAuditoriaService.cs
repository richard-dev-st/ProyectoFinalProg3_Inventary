using Application.DTOs.Auditoria;
using Core.Domain.Entities;

namespace Application.Interfaces
{
    public interface IAuditoriaService
    {
        /// <summary>
        /// RF-AUD-01: prepara el registro de auditoría sin guardarlo. El caso de uso que originó
        /// el evento lo agrega al mismo contexto y hace un único SaveChanges, para que el cambio
        /// y su registro sean una unidad de trabajo.
        /// </summary>
        RegistroAuditoria Preparar(Guid usuarioId, string usuarioEmail, string accion,
            string entidad, string entidadId,
            string? valorAnterior = null, string? valorNuevo = null,
            DateTime? fechaHoraUtc = null);

        /// <summary>
        /// RF-AUD-07 / RD-06: consulta filtrable por usuario, entidad y rango de fechas,
        /// reservada al Administrador. La comprobación del rol vive en el servicio para que
        /// un Estándar también sea rechazado si se invoca directamente.
        /// </summary>
        Task<ResultadoAuditoriaDto> ConsultarAsync(
            bool esAdministrador, FiltroAuditoriaDto filtro, CancellationToken ct = default);
    }
}
