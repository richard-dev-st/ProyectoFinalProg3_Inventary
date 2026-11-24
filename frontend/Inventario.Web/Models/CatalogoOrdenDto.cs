namespace Inventario.Web.Models
{
    // RF-NEG-02: espejo de Application.DTOs.Ordenes.CatalogoOrdenDto.
    public record ProveedorOpcionDto(int Id, string Nombre);

    public record ProductoOpcionDto(
        int Id,
        string Nombre,
        string Codigo,
        decimal Precio,
        int ProveedorId
    );

    public record CatalogoOrdenDto(
        List<ProveedorOpcionDto> Proveedores,
        List<ProductoOpcionDto> Productos
    );
}
