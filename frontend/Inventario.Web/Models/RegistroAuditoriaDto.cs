namespace Inventario.Web.Models
{
    // RF-AUD-01: espejo de Core.Application/DTOs/Auditoria/RegistroAuditoriaDto.
    public record RegistroAuditoriaDto(
        long Id,
        Guid UsuarioId,
        string UsuarioEmail,
        string Accion,
        string Entidad,
        string EntidadId,
        DateTime FechaHoraUtc,
        string? ValorAnterior = null,
        string? ValorNuevo = null);
}
