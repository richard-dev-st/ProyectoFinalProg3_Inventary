using Application.DTOs.Divisa;
using Application.DTOs.Ordenes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IOrdenCompraService
    {
        //Crear orden en borrador
        Task<int> CrearBorradorAsync(CrearOrdenCompraDto dto, Guid usuarioId);
        Task<TotalEnDopDto> ObtenerTotalEnDopAsync(int ordenId, CancellationToken ct = default);
        Task EnviarAprobacionAsync(int id);
        Task AprobarAsync(int id);
        Task RecibirAsync(int id, Guid usuarioId);
        Task CancelarAsync(int id);
    }
}
