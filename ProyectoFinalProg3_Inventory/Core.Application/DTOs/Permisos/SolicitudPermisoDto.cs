using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Permisos
{
    public record SolicitudPermisoDto
    {
        public Guid Id { get; init; }
        public Guid SolicitanteUsuarioId { get; init; }
        public string Permiso { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public DateTime CreadoEn { get; init; }
        public Guid? ResueltoPorUsuarioId { get; init; }   // RF-GP-03
        public DateTime? FechaResolucion { get; init; }
        public string? MotivoRechazo { get; init; }
    }
}
