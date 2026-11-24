namespace Inventario.Web.Models
{
    // Espejo de Application.DTOs.Catalogo.ProductoDto.
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
}
