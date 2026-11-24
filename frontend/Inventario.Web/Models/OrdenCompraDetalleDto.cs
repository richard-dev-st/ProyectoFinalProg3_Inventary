namespace Inventario.Web.Models
{
    // Espejo de Application.DTOs.Ordenes.DetalleOrdenLineaDto.
    public record DetalleOrdenLineaDto(
        int ProductoId,
        string ProductoNombre,
        string ProductoCodigo,
        int Cantidad,
        decimal Precio,
        decimal Subtotal
    );

    // Espejo de Application.DTOs.Ordenes.OrdenCompraDetalleDto.
    public record OrdenCompraDetalleDto(
        int Id,
        int ProveedorId,
        string ProveedorNombre,
        string Estado,
        decimal Total,
        DateTime FechaCreacion,
        Guid CreadoPorUsuarioId,
        List<DetalleOrdenLineaDto> Detalles
    );
}
