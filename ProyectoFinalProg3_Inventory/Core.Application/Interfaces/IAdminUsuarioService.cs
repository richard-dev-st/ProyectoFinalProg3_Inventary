using Application.DTOs.Admin;
using Core.Application.DTOs.Admin;

namespace Application.Interfaces
{
    public interface IAdminUsuarioService
    {
        Task<IEnumerable<UsuarioAdminDto>> ObtenerUsuariosAsync();
        Task CambiarRolUsuarioAsync(Guid usuarioId, CambiarRolDto dto, Guid adminIdActual);
        Task CambiarEstadoUsuarioAsync(Guid usuarioId, CambiarEstadoDto dto, Guid adminIdActual);
    }
}