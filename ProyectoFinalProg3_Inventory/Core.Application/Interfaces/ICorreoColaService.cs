using Application.DTOs.Correos;

namespace Application.Interfaces
{
    public interface ICorreoColaService
    {
        /// <summary>RF-NOT-11: consulta de la cola, opcionalmente filtrada por estado.</summary>
        Task<IEnumerable<CorreoEnColaDto>> ObtenerAsync(string? estado = null);
    }
}
