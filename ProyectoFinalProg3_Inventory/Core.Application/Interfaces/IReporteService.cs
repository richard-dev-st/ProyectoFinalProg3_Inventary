using Application.DTOs.Reportes;

namespace Application.Interfaces
{
    public interface IReporteService
    {
        /// <summary>
        /// RF-REP-01/04/05: agregado de solicitudes por estado. Un Estándar solo ve las suyas.
        /// </summary>
        Task<ReporteSolicitudesDto> ObtenerSolicitudesAsync(
            Guid usuarioId,
            bool esAdministrador,
            DateTime? desde = null,
            DateTime? hasta = null);

        /// <summary>
        /// RF-REP-02/04/05: agregado de órdenes por proveedor y estado. Un Estándar solo ve las suyas.
        /// </summary>
        Task<ReporteOrdenesDto> ObtenerOrdenesAsync(
            Guid usuarioId,
            bool esAdministrador,
            DateTime? desde = null,
            DateTime? hasta = null);
    }
}
