namespace Inventario.Web.Models
{
    // RF-NOT-04: espejo de Core.Application/DTOs/Notificaciones/NotificacionDto.
    public record NotificacionDto
    (
        Guid Id,
        string Mensaje,
        string Tipo,
        bool Leida,
        DateTime FechaCreacion,
        string? ReferenciaId
    );
}
