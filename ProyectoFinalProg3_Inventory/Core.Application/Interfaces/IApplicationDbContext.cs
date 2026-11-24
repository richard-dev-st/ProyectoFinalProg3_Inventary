using Application.Interfaces;
using Business.Inventory.Entities;
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
        DbSet<TokenRevocado> TokensRevocados { get; set; }
        DbSet<SolicitudPermiso> SolicitudesPermiso { get; set; }
        DbSet<PermisoUsuario> PermisosUsuario { get; set; }
        DbSet<Documento> Documentos { get; set; }
        DbSet<Notificacion> Notificaciones { get; set; }
        DbSet<OrdenCompra> OrdenesCompra { get; set; }
        DbSet<Proveedor> Proveedores { get; set; }
        DbSet<Producto> Productos { get; set; }
        DbSet<RegistroAuditoria> Auditoria { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}