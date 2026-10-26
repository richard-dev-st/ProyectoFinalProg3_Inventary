using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Ordenes
{
    public record DetalleOrdenDto
    {
        public int ProductoId { get; init; }
        public int Cantidad { get; init; }
        public decimal Precio { get; init; }
    }
}
