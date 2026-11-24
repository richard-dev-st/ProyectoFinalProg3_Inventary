namespace Inventario.Web.Models
{
    // RF-NEG-02: espejo de Application.DTOs.Ordenes.DetalleOrdenDto.
    public class DetalleOrdenRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
    }
}
