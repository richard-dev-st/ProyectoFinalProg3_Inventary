namespace Inventario.Web.Models
{
    // Espejo de Application.DTOs.Catalogo.CrearProductoDto.
    public class CrearProductoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public decimal Precio { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public int ProveedorId { get; set; }
    }
}
