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
        public string Permiso { get; private set; } = string.Empty;
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
        public SolicitudPermiso(Guid solicitanteUsuarioId, string permiso, string descripcion)
        {

            if (string.IsNullOrWhiteSpace(permiso))
            {
                throw new InvalidOperationException("El permiso solicitado es obligatorio.");
            }

            // Hallazgo 3: no se puede guardar un valor que no cabe en la columna.
            permiso = permiso.Trim();
            if (permiso.Length > 100)
            {
                throw new InvalidOperationException("El permiso no puede superar los 100 caracteres.");
            }

            descripcion ??= string.Empty;
            if (descripcion.Length > 500)
            {
                throw new InvalidOperationException("La descripción no puede superar los 500 caracteres.");
            }

            Id = Guid.NewGuid();
            SolicitanteUsuarioId = solicitanteUsuarioId;
            Permiso = permiso;
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
            // Hallazgo 3: el motivo se guarda en nvarchar(500).
            if (motivo.Length > 500)
            {
                throw new InvalidOperationException("El motivo de rechazo no puede superar los 500 caracteres.");
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

        internal void CambiarEstado(EstadoSolicitudPermiso nuevo)
        {
            if (!TransicionesPermitidas[Estado].Contains(nuevo))
            {
                throw new InvalidOperationException($"Transición inválida: {Estado} -> {nuevo}.");
            }

            Estado = nuevo; //Solo aqui se cambia el estado
        }
    }
}
