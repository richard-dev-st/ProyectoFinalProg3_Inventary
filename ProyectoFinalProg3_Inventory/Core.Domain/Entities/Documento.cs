using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.Entities
{
    public class Documento
    {
        public Guid Id { get; private set; }
        public string NombreOriginal { get; private set; } = string.Empty;
        public string NombreAlmacenado { get; private set; } = string.Empty;
        public string TipoContenido { get; private set; } = string.Empty;
        public long TamanioBytes { get; private set; }
        public DateTime FechaSubida { get; private set; }
        public Guid SubidoPorUsuarioId { get; private set; }
        public Usuario? SubidoPorUsuario { get; private set; }
        public bool EsEliminado { get; private set; }
        public DateTime? FechaEliminacion { get; private set; }

        public Documento(string nombreOriginal, string nombreAlmacenado, string tipoContenido, long tamanioBytes, Guid subidoPorUsuarioId, DateTime fechaSubida)
        {
            Id = Guid.NewGuid();
            NombreOriginal = nombreOriginal;
            NombreAlmacenado = nombreAlmacenado;
            TipoContenido = tipoContenido;
            TamanioBytes = tamanioBytes;
            FechaSubida = fechaSubida;
            SubidoPorUsuarioId = subidoPorUsuarioId;
            EsEliminado = false;
            FechaEliminacion = null;
        }

        private Documento()
        {
        }

        public void Eliminar()
        {
            if (EsEliminado)
            {
                return;
            }
            EsEliminado = true;
            FechaEliminacion = DateTime.UtcNow;
        }
    }
}
