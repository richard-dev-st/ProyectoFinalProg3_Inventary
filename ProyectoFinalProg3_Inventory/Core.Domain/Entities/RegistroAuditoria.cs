using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.Entities
{
    public class RegistroAuditoria
    {
        public long Id { get; private set; } //identificador propio
        public Guid UsuarioId { get; private set; } //identificador del usuario que realizó la acción
        public string UsuarioEmail { get; private set; } = default!; // desnormalización del email del usuario para no depender de la tabla de usuarios
        public string Accion { get; private set; } = default!; //Vocabulario de acciones: Crear, Modificar, Eliminar, IniciarSesion, CerrarSesion, etc.
        public string Entidad { get; private set; } = default!; //Nombre de la entidad afectada
        public string EntidadId { get; private set; } = default!; //Identificador de la entidad afectada
        public DateTime FechaHoraUtc { get; private set; } //Fecha y hora de la acción en UTC
        public string? ValorAnterior { get; private set; } //Valor anterior de la entidad (si aplica)
        public string? ValorNuevo { get; private set; } //Valor nuevo de la entidad (si aplica)

        private RegistroAuditoria() { } //Constructor vacío para EF Core

        public static RegistroAuditoria Crear(Guid usuarioId, string usuarioEmail, string accion, string entidad, string entidadId, DateTime fechaHoraUtc, string? valorAnterior = null, string? valorNuevo = null)
        {
            // RF-AUD-01: Si falta alguno de los campos obligatorios, no se crea el registro.
            if (usuarioId == Guid.Empty) throw new InvalidOperationException("Falta el usuario que actuó.");
            if (string.IsNullOrWhiteSpace(usuarioEmail)) throw new InvalidOperationException("Falta el email del usuario que actuó.");
            if (string.IsNullOrWhiteSpace(accion)) throw new InvalidOperationException("Falta la acción.");
            if (string.IsNullOrWhiteSpace(entidad)) throw new InvalidOperationException("Falta la entidad.");
            if (string.IsNullOrWhiteSpace(entidadId)) throw new InvalidOperationException("Falta el identificador de la entidad.");
            if (fechaHoraUtc == default) throw new InvalidOperationException("Falta la fecha y hora de la acción.");

            return new RegistroAuditoria
            {
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail,
                Accion = accion,
                Entidad = entidad,
                EntidadId = entidadId,
                FechaHoraUtc = fechaHoraUtc,
                ValorAnterior = valorAnterior,
                ValorNuevo = valorNuevo
            };
        }
    }
}
