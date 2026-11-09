using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Divisa
{
    public record TasaCambioDto(
        decimal Tasa,
        //Moneda de origen
        string MonedaOrigen,
        //Moneda de destino
        string MonedaDestino,
        //Fecha de actualización
        DateTime ActualizadoEn);
}
