using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Documentos
{
    public record DocumentoDescargaDto(
        string NombreOriginal,
        string TipoContenido,
        Stream Contenido);
}
