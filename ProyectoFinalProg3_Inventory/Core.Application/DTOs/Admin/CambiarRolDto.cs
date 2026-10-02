using Core.Domain.Entities.Enums;
namespace Core.Application.DTOs.Admin
{
    public record CambiarRolDto
    {
        public RolUsuario NuevoRol { get; set; }
    }
}