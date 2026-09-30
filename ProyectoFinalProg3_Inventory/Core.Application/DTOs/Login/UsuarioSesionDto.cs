using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Login
{
    public record UsuarioSesionDto(
        Guid Id,
        string Email,
        string Rol,
        bool Activo
    );
}