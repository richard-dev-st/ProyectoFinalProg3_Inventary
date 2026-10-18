using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Permisos
{
    public record RechazarSolicitudDto
    {
        public string Motivo { get; init; } = string.Empty;
    }
}
