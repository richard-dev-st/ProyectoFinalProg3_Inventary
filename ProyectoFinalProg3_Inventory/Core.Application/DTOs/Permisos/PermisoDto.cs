using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Permisos
{
    public record PermisoDto
    {
        public Guid Id { get; init; }
        public string Permiso { get; init; } = string.Empty;
        public DateTime ConcedidoEn { get; init; }
        public Guid SolicitudId { get; init; }
    }
}
