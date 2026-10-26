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
        Task EnviarAprobacionAsync(int id);
        Task AprobarAsync(int id);
        Task RecibirAsync(int id);
        Task CancelarAsync(int id);
    }
}
