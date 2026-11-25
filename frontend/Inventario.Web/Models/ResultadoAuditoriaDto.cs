namespace Inventario.Web.Models
{
    // RF-AUD-07: espejo de Core.Application/DTOs/Auditoria/ResultadoAuditoriaDto.
    public record ResultadoAuditoriaDto(
        List<RegistroAuditoriaDto> Registros,
        int Total,
        int Pagina,
        int TamanioPagina);
}
