namespace Inventario.Web.Models
{
    // Espejo de Application.DTOs.Catalogo.CrearProveedorDto.
    public class CrearProveedorRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }
}
