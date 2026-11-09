using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Divisa
{
    public record TotalEnDopDto(
        //Origen de la moneda
        int OrdenId,
        //Total en dolares
        decimal TotalUsd,
        //Tasa
        decimal Tasa,
        //Total en pesos dominicanos
        decimal TotalDop,
        //Fecha de actualización
        DateTime ActualizadoEn
    );
}
