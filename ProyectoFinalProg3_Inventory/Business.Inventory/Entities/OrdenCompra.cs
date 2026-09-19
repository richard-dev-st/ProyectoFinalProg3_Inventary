using System;
using System.Collections.Generic;
using System.Text;

namespace Business.Inventory.Entities
{
    public class OrdenCompra
    {
        public int Id { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        //public Estado estado { get; set; } = Estado.Borrador;
        public decimal Total { get; set; }

        //Referencia al usuario del Core que creo la orden (Sin aclopar la entidad Usuario)
        public int CreadoPorUsuarioId { get; set; }

        //Clave foranea y relacion con Proveedor
        public int ProveedorId { get; set; }
        public Proveedor Proveedor { get; set; } = null!;

        //RELACION UNO A MUCHOS CON DETALLE DE ORDENES
        public List<DetalleOrden> Detalles { get; set; } = new List<DetalleOrden>();
    }
}
