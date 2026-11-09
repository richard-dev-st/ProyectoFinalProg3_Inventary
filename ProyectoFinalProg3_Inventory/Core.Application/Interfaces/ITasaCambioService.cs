using Application.DTOs.Divisa;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ITasaCambioService
    {
        //El cancellationToken permite cancelar la operación si es necesario, por ejemplo, si el usuario cierra la aplicación antes de que se complete la solicitud.
        Task<TasaCambioDto> ObtenerTasaAsync(string origen, string destino, CancellationToken ct = default);
    }
}
