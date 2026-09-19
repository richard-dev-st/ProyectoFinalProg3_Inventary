using System;
using System.Collections.Generic;
using System.Text;

namespace Business.Inventory.Entities
{
    public class Proveedor
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        //Relaciones
        public List<Producto> Productos { get; set; } = new List<Producto>();
        public List<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();
    }
}
