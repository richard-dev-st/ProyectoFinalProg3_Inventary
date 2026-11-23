using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auditoria
{
    public sealed record ResultadoAuditoriaDto(
        IEnumerable<RegistroAuditoriaDto> Registros,
        int Total, int Pagina, int TamanioPagina);
}
