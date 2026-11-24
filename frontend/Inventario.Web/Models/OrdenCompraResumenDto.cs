namespace Inventario.Web.Models
{
    // RF-NEG-02: espejo de Application.DTOs.Ordenes.OrdenCompraResumenDto.
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
