namespace Inventario.Web.Models
{
    public record UsuarioAdmin(Guid Id, string Email, RolUsuario Rol, bool Activo)
    {
        public string RolNombre => Rol == RolUsuario.Administrador
            ? "Administrador"
            : "Estándar";
    }
}