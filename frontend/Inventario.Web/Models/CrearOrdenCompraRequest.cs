namespace Inventario.Web.Models
{
    // RF-NEG-02: espejo de Application.DTOs.Ordenes.CrearOrdenCompraDto.
    public class CrearOrdenCompraRequest
    {
        public int ProveedorId { get; set; }
        // El backend espera la propiedad "ordenes" (camelCase al serializar).
        public List<DetalleOrdenRequest> Ordenes { get; set; } = new();
    }
}
