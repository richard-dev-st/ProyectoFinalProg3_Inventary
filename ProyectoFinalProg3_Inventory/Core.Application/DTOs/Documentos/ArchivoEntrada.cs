using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Documentos
{
    public record ArchivoEntrada(
        string NombreOriginal,
        string TipoContenido,
        long Longitud,
        Stream Contenido);
}
