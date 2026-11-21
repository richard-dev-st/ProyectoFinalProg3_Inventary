using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public class RestablecerPasswordRequest
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        // RF-CA-10: el backend genera un código de 6 dígitos con vencimiento de 15 minutos.
        [Required(ErrorMessage = "El código de recuperación es obligatorio.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener 6 dígitos.")]
        public string Codigo { get; set; } = string.Empty;

        // RF-CA-14: la misma política que valida el backend.
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener al menos una letra y un número.")]
        public string NuevaPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contraseña.")]
        [Compare(nameof(NuevaPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
