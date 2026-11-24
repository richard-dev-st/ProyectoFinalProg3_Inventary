namespace Application.DTOs.Ordenes
{
    // RF-NEG-02: resumen de una orden para el listado del frontend.
    // No expone la entidad de EF ni sus detalles completos (RD-06).
    public record OrdenCompraResumenDto(
        int Id,
        int ProveedorId,
        string Estado,
        decimal Total,
        DateTime FechaCreacion,
        Guid CreadoPorUsuarioId,
        int CantidadDetalles
    );
}
