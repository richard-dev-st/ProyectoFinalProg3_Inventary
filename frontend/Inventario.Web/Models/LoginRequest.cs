using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "El email debe ser obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }
}