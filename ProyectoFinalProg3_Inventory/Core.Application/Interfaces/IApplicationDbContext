using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Usuario> Usuarios { get; set; }
        DbSet<CorreoEnCola> CorreosEnCola { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}