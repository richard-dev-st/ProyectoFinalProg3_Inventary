namespace Inventario.Web.Models
{
    // RF-REP-01: solicitudes agrupadas por estado dentro de un período.
    public record ReporteSolicitudesDto
    (
        DateTime? Desde,
        DateTime? Hasta,
        int Total,
        List<GrupoSolicitudesDto> Grupos
    );

    public record GrupoSolicitudesDto(string Estado, int Cantidad);
}
