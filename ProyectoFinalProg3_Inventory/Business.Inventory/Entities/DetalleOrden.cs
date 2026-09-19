using System;
using System.Collections.Generic;
using System.Text;

namespace Business.Inventory.Entities
{
    public class DetalleOrden
    {
        public int Id { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Cantidad * Precio;

        //Relaciones con ordenCompra y producto
        public int OrdenCompraId { get; set; }
        public OrdenCompra OrdenCompra { get; set; } = null!;

        public int ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;
    }
}
