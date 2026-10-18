using Application.DTOs.Permisos;
using Core.Domain.Entities;

namespace Application.Interfaces
{
    public interface ISolicitudPermisoService
    {
        Task<SolicitudPermiso> CrearAsync(Guid usuarioId, string permiso, string descripcion);
        Task<IEnumerable<SolicitudPermiso>> PendientesAsync();
        Task<IEnumerable<SolicitudPermiso>> MiasAsync(Guid usuarioId);
        Task<SolicitudPermiso?> AprobarAsync(Guid solicitudId, Guid usuarioId);          // null = 404; InvalidOperationException del dominio → 400
        Task<SolicitudPermiso?> RechazarAsync(Guid solicitudId, Guid usuarioId, string motivo); // null = 404; InvalidOperationException del dominio → 400
        Task<IEnumerable<PermisoDto>> MisPermisosAsync(Guid usuarioId);
    }
}