namespace Application.DTOs.Catalogo
{
    // Datos maestros del módulo de negocio: proveedores y productos.
    // Sin ellos no se puede crear una orden de compra.
    public record ProveedorDto(
        int Id,
        string Nombre,
        string Cedula,
        string Telefono,
        string Email,
        int CantidadProductos
    );

    public record CrearProveedorDto(
        string Nombre,
        string? Cedula,
        string? Telefono,
        string? Email
    );

    public record ProductoDto(
        int Id,
        string Nombre,
        string Codigo,
        decimal Precio,
        int StockActual,
        int StockMinimo,
        int ProveedorId,
        string ProveedorNombre
    );

    public record CrearProductoDto(
        string Nombre,
        string? Codigo,
        decimal Precio,
        int StockActual,
        int StockMinimo,
        int ProveedorId
    );
}
