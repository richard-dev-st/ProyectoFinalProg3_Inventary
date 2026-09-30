using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Login
{
    public record LoginResponseDto(
        string Token,
        string Email,
        string Rol,
        DateTime Expiracion
    );
}