using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.Entities
{
    public class PermisoUsuario
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public string Permiso { get; private set; } = string.Empty;
        public DateTime ConcedidoEn { get; private set; }
        public Guid SolicitudId { get; private set; }

        private PermisoUsuario() { }

        public PermisoUsuario(Guid usuarioId, string permiso, Guid solicitudId, DateTime concedidoEn)
        {
            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            Permiso = permiso;
            SolicitudId = solicitudId;
            ConcedidoEn = concedidoEn;
        }
    }
}
