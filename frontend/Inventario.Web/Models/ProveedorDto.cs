namespace Inventario.Web.Models
{
    // Espejo de Application.DTOs.Catalogo.ProveedorDto.
    public record ProveedorDto(
        int Id,
        string Nombre,
        string Cedula,
        string Telefono,
        string Email,
        int CantidadProductos
    );
}
