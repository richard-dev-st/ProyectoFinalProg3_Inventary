namespace Inventario.Web.Models
{
    // RF-NOT-11: espejo de Core.Application/DTOs/Correos/CorreoEnColaDto.
    public record CorreoEnColaDto
    (
        Guid Id,
        string Destinatario,
        string Asunto,
        string Estado,
        int Intentos,
        DateTime FechaCreacion,
        DateTime? FechaEnvio,
        DateTime? FechaUltimoIntento,
        string? UltimoError
    );
}
