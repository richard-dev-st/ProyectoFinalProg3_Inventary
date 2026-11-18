using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public record UsuarioSesion
    (
        Guid Id,
        string Email,
        string Rol,
        bool Activo
    );
}