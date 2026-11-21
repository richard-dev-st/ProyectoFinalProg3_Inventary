namespace Inventario.Web.Models
{
    public record CambiarRolRequest
    {
        public RolUsuario NuevoRol { get; set; }
    }
}