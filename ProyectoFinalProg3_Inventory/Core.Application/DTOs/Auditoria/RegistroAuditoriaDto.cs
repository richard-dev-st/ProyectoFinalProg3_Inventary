using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auditoria
{
    public sealed record RegistroAuditoriaDto(
        long Id, Guid UsuarioId, string UsuarioEmail, string Accion, string Entidad, string EntidadId, DateTime FechaHoraUtc, string? ValorAnterior = null, string? ValorNuevo = null);
}
