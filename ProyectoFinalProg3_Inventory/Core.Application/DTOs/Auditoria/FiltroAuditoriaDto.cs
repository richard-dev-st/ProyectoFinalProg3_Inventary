using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auditoria
{
    public sealed record FiltroAuditoriaDto(
        Guid? UsuarioId,
        string? Entidad,
        DateTime? Desde,
        DateTime? Hasta,
        int Pagina = 1,
        int TamanioPagina = 20);
}
