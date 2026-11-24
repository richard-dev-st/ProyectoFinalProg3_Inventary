namespace Application.DTOs.Ordenes
{
    // RF-NEG-02: opciones de solo lectura para el formulario de órdenes.
    // Evita que el usuario teclee ids que no existen (lo que provocaba un error de FK).
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
