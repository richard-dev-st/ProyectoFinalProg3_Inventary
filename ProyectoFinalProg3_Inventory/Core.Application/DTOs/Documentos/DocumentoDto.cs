using Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Documentos
{
    public record DocumentoDto(
        Guid Id,
        string NombreOriginal,
        string TipoContenido,
        long TamanioBytes,
        DateTime FechaSubida,
        Guid SubidoPorUsuarioId,
        string SubidoPorEmail,
        bool EsEliminado);
}
