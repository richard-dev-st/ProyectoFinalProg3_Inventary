using System;
using System.Collections.Generic;
using System.Text;
using Core.Domain.Entities.Enums;

namespace Business.Inventory.Entities
{
    public class OrdenCompra
    {
        public int Id { get; set; }
        public EstadoOrden Estado { get; private set; } = EstadoOrden.Borrador;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        //public Estado estado { get; set; } = Estado.Borrador;
        public decimal Total { get; set; }

        //Referencia al usuario del Core que creo la orden (Sin aclopar la entidad Usuario)
        public Guid CreadoPorUsuarioId { get; set; }

        //Clave foranea y relacion con Proveedor
        public int ProveedorId { get; set; }
        public Proveedor Proveedor { get; set; } = null!;

        //RELACION UNO A MUCHOS CON DETALLE DE ORDENES
        public List<DetalleOrden> Detalles { get; set; } = new List<DetalleOrden>();

        // RD-04: Transiciones permitidas declaradas en UN SOLO LUGAR (en la entidad)
        private static readonly Dictionary<EstadoOrden, List<EstadoOrden>> TransicionesPermitidas = new()
        {
            { EstadoOrden.Borrador, new() { EstadoOrden.PendienteAprobacion, EstadoOrden.Cancelada } },
            { EstadoOrden.PendienteAprobacion, new() { EstadoOrden.Aprobada, EstadoOrden.Cancelada } },
            { EstadoOrden.Aprobada, new() { EstadoOrden.Recibida, EstadoOrden.Cancelada } },
            { EstadoOrden.Recibida, new() }, // Estado terminal (RF-NEG-05): sin salidas
            { EstadoOrden.Cancelada, new() } // Estado terminal (RF-NEG-05): sin salidas
        };

        // Métodos de comportamiento de negocio para encapsular las transiciones
        public void EnviarAAprobacion() => CambiarEstado(EstadoOrden.PendienteAprobacion);
        public void Aprobar() => CambiarEstado(EstadoOrden.Aprobada);
        public void Recibir() => CambiarEstado(EstadoOrden.Recibida);
        public void Cancelar() => CambiarEstado(EstadoOrden.Cancelada);

        private void CambiarEstado(EstadoOrden nuevoEstado)
        {
            // RF-NEG-04: Transición prohibida explícita
            if (!TransicionesPermitidas[Estado].Contains(nuevoEstado))
            {
                throw new InvalidOperationException($"Transición inválida: No se puede cambiar el estado de {Estado} a {nuevoEstado}.");
            }
            Estado = nuevoEstado;
        }
    }
}
