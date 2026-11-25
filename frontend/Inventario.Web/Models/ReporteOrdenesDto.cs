namespace Inventario.Web.Models
{
    // RF-REP-02: compras por proveedor y estado dentro de un período.
    public record ReporteOrdenesDto
    (
        DateTime? Desde,
        DateTime? Hasta,
        int TotalOrdenes,
        decimal MontoTotalUsd,
        List<GrupoOrdenesDto> Grupos
    );

    public record GrupoOrdenesDto
    (
        int ProveedorId,
        string Proveedor,
        string Estado,
        int CantidadOrdenes,
        decimal MontoTotalUsd
    );
}
