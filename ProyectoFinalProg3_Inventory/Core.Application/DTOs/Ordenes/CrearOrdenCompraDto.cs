using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Ordenes
{
    public record CrearOrdenCompraDto
    {
        public int ProveedorId { get; init; }
        public List<DetalleOrdenDto> ordenes { get; init; } = new();
    }
}
