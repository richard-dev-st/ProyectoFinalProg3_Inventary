namespace Inventario.Web.Models
{
    // Espejo de Application.DTOs.Documentos.DocumentoDto (RF-DOC-01..04).
    public record DocumentoDto(
        Guid Id,
        string NombreOriginal,
        string TipoContenido,
        long TamanioBytes,
        DateTime FechaSubida,
        Guid SubidoPorUsuarioId,
        string SubidoPorEmail,
        bool EsEliminado
    );
}
