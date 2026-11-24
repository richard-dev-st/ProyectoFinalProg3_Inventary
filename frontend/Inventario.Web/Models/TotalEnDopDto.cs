namespace Inventario.Web.Models
{
    // RF-NEG-02: espejo de Application.DTOs.Divisa.TotalEnDopDto.
    public record TotalEnDopDto(
        int OrdenId,
        decimal TotalUsd,
        decimal Tasa,
        decimal TotalDop,
        DateTime ActualizadoEn
    );
}
