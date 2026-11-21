namespace Inventario.Web.Models
{
    public record CambiarEstadoRequest
    {
        public bool Activo { get; set; }
    }
}