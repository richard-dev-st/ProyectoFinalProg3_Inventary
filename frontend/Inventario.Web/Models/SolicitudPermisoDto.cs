namespace Inventario.Web.Models
{
    // RF-GP-01..07: espejo de Core.Application/DTOs/Permisos/SolicitudPermisoDto.
    public record SolicitudPermisoDto
    (
        Guid Id,
        Guid SolicitanteUsuarioId,
        string Permiso,
        string Descripcion,
        string Estado,
        DateTime CreadoEn,
        Guid? ResueltoPorUsuarioId,
        DateTime? FechaResolucion,
        string? MotivoRechazo
    );
}
