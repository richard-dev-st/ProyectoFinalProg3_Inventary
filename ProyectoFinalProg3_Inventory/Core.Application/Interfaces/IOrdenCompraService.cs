using Application.DTOs.Divisa;
using Application.DTOs.Ordenes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IOrdenCompraService
    {
        // RF-NEG-02: listado de órdenes para la UI (Estándar solo ve las suyas).
        Task<List<OrdenCompraResumenDto>> ListarAsync(Guid usuarioId, bool esAdministrador);

        // RF-NEG-02: detalle de una orden con sus líneas y el proveedor.
        Task<OrdenCompraDetalleDto?> ObtenerDetalleAsync(int id, Guid usuarioId, bool esAdministrador);

        // RF-NEG-02: edición de una orden mientras siga en Borrador.
        Task ActualizarBorradorAsync(int id, CrearOrdenCompraDto dto, Guid usuarioId, bool esAdministrador);

        //Crear orden en borrador
        Task<int> CrearBorradorAsync(CrearOrdenCompraDto dto, Guid usuarioId);
        Task<TotalEnDopDto> ObtenerTotalEnDopAsync(int ordenId, CancellationToken ct = default);
        Task EnviarAprobacionAsync(int id);
        Task AprobarAsync(int id);
        Task RecibirAsync(int id, Guid usuarioId);
        Task CancelarAsync(int id);
    }
}
