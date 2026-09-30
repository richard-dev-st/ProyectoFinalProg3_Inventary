using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerarToken(Usuario usuario);
    }
}