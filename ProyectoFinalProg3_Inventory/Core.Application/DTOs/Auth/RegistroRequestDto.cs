using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    //Dto para recibir la peticion de registro
    public record RegistroRequestDto(
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        string Email,

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener al menos una letra y un número.")]
        string Password,

        [Required(ErrorMessage = "La URL base es requerida.")]
        string BaseUrl);

    //Dto para enviar la respuesta del servicio
    public record AuthResponseDto(bool Exito, string Mensaje);
}