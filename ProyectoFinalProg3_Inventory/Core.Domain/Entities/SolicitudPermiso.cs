using Core.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.Entities
{
    public class SolicitudPermiso
    {
        public Guid Id { get; private set; }
        public EstadoSolicitudPermiso Estado { get; private set; } = EstadoSolicitudPermiso.Pendiente;
        public Guid SolicitanteUsuarioId { get; private set; }
        public string Descripcion { get; private set; } = string.Empty;
        public DateTime CreadoEn { get; private set; }
        public Guid? ResueltoPorUsuarioId { get; private set; }
        public DateTime? FechaResolucion { get; private set; }
        public string MotivoRechazo { get; private set; } = string.Empty;

        //RD-04 Todas las transiciones en un solo lugar
        private static readonly Dictionary<EstadoSolicitudPermiso, List<EstadoSolicitudPermiso>> TransicionesPermitidas = new()
        {
            { EstadoSolicitudPermiso.Pendiente, new(){ EstadoSolicitudPermiso.Aprobada, EstadoSolicitudPermiso.Rechazada } },
            { EstadoSolicitudPermiso.Aprobada, new() {EstadoSolicitudPermiso.Aplicada } },
            { EstadoSolicitudPermiso.Aplicada, new()},
            { EstadoSolicitudPermiso.Rechazada, new()}
        };

        private SolicitudPermiso() { }
        public SolicitudPermiso(Guid solicitanteUsuarioId, string descripcion)
        {
            Id = Guid.NewGuid();
            SolicitanteUsuarioId = solicitanteUsuarioId;
            Descripcion = descripcion;
            CreadoEn = DateTime.UtcNow;
        }

        public void Aprobar(Guid resueltoPorUsuarioId, DateTime fecha)
        {
            if(SolicitanteUsuarioId == resueltoPorUsuarioId)
            {
                throw new InvalidOperationException("El solicitante no puede aprobar su propia solicitud.");
            }

            CambiarEstado(EstadoSolicitudPermiso.Aprobada); //Valida la transicion
            ResueltoPorUsuarioId = resueltoPorUsuarioId; //RF: Registrar quien
            FechaResolucion = fecha;

            CambiarEstado(EstadoSolicitudPermiso.Aplicada);
        }

        public void Rechazar(Guid resueltoPorUsuarioId, string motivo, DateTime fecha)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw new InvalidOperationException("El motivo de rechazo es obligatorio.");
            }
            if(SolicitanteUsuarioId == resueltoPorUsuarioId)
            {
                throw new InvalidOperationException("No puedes resolver tu propia solicitud.");
            }

            CambiarEstado(EstadoSolicitudPermiso.Rechazada);
            ResueltoPorUsuarioId = resueltoPorUsuarioId;
            FechaResolucion = fecha;
            MotivoRechazo = motivo;
        }

        private void CambiarEstado(EstadoSolicitudPermiso nuevo)
        {
            if (!TransicionesPermitidas[Estado].Contains(nuevo))
            {
                throw new InvalidOperationException($"Transición inválida: {Estado} -> {nuevo}.");
            }

            Estado = nuevo; //Solo aqui se cambia el estado
        }
    }
}
