using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public class RegistroRequest
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        // RF-CA-14: la misma política que valida el backend (RF-CA-02 hashea el valor).
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener al menos una letra y un número.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = string.Empty;

        // RF-CA-15: la API exige el campo aunque luego usa su propia URL configurada.
        public string BaseUrl { get; set; } = string.Empty;
    }
}
