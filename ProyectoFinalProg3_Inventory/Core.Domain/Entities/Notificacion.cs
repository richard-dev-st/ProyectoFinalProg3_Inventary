namespace Core.Domain.Entities
{
    /// <summary>
    /// Notificación interna de un usuario (RF-NOT-04). La copia por correo se encola
    /// como <see cref="CorreoEnCola"/> según la preferencia del destinatario (RF-NOT-06/07).
    /// </summary>
    public class Notificacion
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public string Mensaje { get; private set; } = string.Empty;
        public string Tipo { get; private set; } = string.Empty;
        public bool Leida { get; private set; }
        public DateTime FechaCreacion { get; private set; }

        /// <summary>
        /// Identificador del registro que originó la notificación (solicitud u orden),
        /// guardado como texto para admitir claves Guid o int.
        /// </summary>
        public string? ReferenciaId { get; private set; }

        private Notificacion() { }

        public Notificacion(Guid usuarioId, string mensaje, string tipo, string? referenciaId = null)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
            {
                throw new ArgumentException("La notificación debe tener un mensaje.", nameof(mensaje));
            }
            if (string.IsNullOrWhiteSpace(tipo))
            {
                throw new ArgumentException("La notificación debe tener un tipo.", nameof(tipo));
            }

            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            Mensaje = mensaje;
            Tipo = tipo;
            Leida = false;
            FechaCreacion = DateTime.UtcNow;
            ReferenciaId = referenciaId;
        }

        /// <summary>RF-NOT-05: marcar como leída es idempotente.</summary>
        public void MarcarLeida()
        {
            Leida = true;
        }
    }
}
