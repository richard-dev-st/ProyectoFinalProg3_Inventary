using Core.Domain.Entities.Enums;

namespace Application.DTOs.Admin
{
    public record UsuarioAdminDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public RolUsuario Rol { get; set; }
        public bool Activo { get; set; }

    }
}