using Application.DTOs.Documentos;
using Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IDocumentoService
    {
        Task<DocumentoDto> SubirDocumentoAsync(ArchivoEntrada archivo, Guid usuarioId);
        Task<IEnumerable<DocumentoDto>> ListarDocumentosAsync(Guid usuarioId);
        Task<DocumentoDescargaDto> DescargarDocumentoAsync(Guid id, Guid usuarioId);
        Task EliminarDocumentoAsync(Guid id, Guid usuarioId);
    }
}
