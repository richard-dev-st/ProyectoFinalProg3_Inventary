namespace Application.DTOs.Ordenes
{
    // RF-NEG-02: detalle de una orden para la pantalla del frontend.
    // Incluye el nombre del proveedor y cada línea con su producto y subtotal.
    public record DetalleOrdenLineaDto(
        int ProductoId,
        string ProductoNombre,
        string ProductoCodigo,
        int Cantidad,
        decimal Precio,
        decimal Subtotal
    );

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
