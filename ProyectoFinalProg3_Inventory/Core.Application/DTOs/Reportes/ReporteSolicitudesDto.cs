namespace Application.DTOs.Reportes
{
    /// <summary>RF-REP-01: solicitudes agrupadas por estado, no el listado de registros.</summary>
    public class ReporteSolicitudesDto
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Total { get; set; }
        public List<GrupoSolicitudesDto> Grupos { get; set; } = new();
    }

    public class GrupoSolicitudesDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
