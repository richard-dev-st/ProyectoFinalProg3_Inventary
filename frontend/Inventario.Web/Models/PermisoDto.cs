namespace Inventario.Web.Models
{
    // RF-GP-05: espejo de Core.Application/DTOs/Permisos/PermisoDto.
    public record PermisoDto
    (
        Guid Id,
        string Permiso,
        DateTime ConcedidoEn,
        Guid SolicitudId
    );
}
