using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IAlmacenDocumentos
    {
        Task GuardarAsync(Stream contenido, string nombreAlmacenado, CancellationToken ct = default);
        Task<Stream> AbrirAsync(string nombreAlmacenado, CancellationToken ct = default);
        bool Existe(string nombreAlmacenado);
        Task EliminarAsync(string nombreAlmacenado, CancellationToken ct = default);
    }
}
