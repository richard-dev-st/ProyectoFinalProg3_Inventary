using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Permisos
{
    public record CrearSolicitudDto
    {
        public string Permiso { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
    }
}
