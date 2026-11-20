namespace Application.DTOs.Reportes
{
    /// <summary>
    /// RF-REP-02: compras por proveedor y estado dentro del período.
    /// Responde cuánto se compra a cada proveedor y en qué estado están esas órdenes.
    /// </summary>
    public class ReporteOrdenesDto
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int TotalOrdenes { get; set; }
        public decimal MontoTotalUsd { get; set; }
        public List<GrupoOrdenesDto> Grupos { get; set; } = new();
    }

    public class GrupoOrdenesDto
    {
        public int ProveedorId { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int CantidadOrdenes { get; set; }
        public decimal MontoTotalUsd { get; set; }
    }
}
