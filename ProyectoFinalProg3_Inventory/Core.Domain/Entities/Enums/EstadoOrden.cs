using System;

namespace Core.Domain.Entities.Enums
{
    public enum EstadoOrden
    {
        Borrador = 1,
        PendienteAprobacion = 2,
        Aprobada = 3,
        Recibida = 4, // Estado terminal (RF-NEG-05)
        Cancelada = 5 // Estado terminal (RF-NEG-05)
    }
}
