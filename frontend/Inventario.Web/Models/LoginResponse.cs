using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public record LoginResponse
    (
        string Token,
        string Email,
        string Rol,
        DateTime Expiracion
    );
}