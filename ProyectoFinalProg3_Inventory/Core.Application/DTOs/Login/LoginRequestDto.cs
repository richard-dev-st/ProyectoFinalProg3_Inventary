using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Login
{
    public record LoginRequestDto
    (
        [Required(ErrorMessage = "El email debe ser obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        string Email,
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        string Password);
}