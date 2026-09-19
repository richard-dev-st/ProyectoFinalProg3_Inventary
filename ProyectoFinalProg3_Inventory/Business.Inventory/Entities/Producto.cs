using System;
using System.Collections.Generic;
using System.Text;

namespace Business.Inventory.Entities
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }

        //Clave foranea y relacion con Proveedor
        public int ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; } = null!;

        //Relacion con detalle de ordenes
        public List<DetalleOrden> DetallesOrden { get; set; } = new List<DetalleOrden>();

    }
}
