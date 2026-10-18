using Application.DTOs.Permisos;
using Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class SolicitudPermisoService : ISolicitudPermisoService
    {
        private readonly IApplicationDbContext _context;

        public SolicitudPermisoService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SolicitudPermiso?> AprobarAsync(Guid solicitudId, Guid usuarioId)
        {
            var s = await _context.SolicitudesPermiso.FindAsync(solicitudId);

            if (s == null)
            {
                return null;
            }

            var fecha = DateTime.UtcNow;
            s.Aprobar(usuarioId, fecha);

            // RF-GP-05: aplicar el cambio de permiso automaticamente en la misma transaccion
            _context.PermisosUsuario.Add(new PermisoUsuario(s.SolicitanteUsuarioId, s.Permiso, s.Id, fecha));

            await _context.SaveChangesAsync();
            return s;
        }

        public async Task<SolicitudPermiso> CrearAsync(Guid usuarioId, string permiso, string descripcion)
        {
            
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
            {
                throw new InvalidOperationException("Usuario no encontrado");
            }

            // Crear la solicitud de permiso
            var solicitud = new SolicitudPermiso(usuarioId, permiso, descripcion);

            // Agregar la solicitud al contexto
            _context.SolicitudesPermiso.Add(solicitud);

            await _context.SaveChangesAsync();
            
            return solicitud;
        }

        public Task<IEnumerable<SolicitudPermiso>> MiasAsync(Guid usuarioId)
        {
            var solicitudes = _context.SolicitudesPermiso
                .Where(s => s.SolicitanteUsuarioId == usuarioId)
                .ToList();

            return Task.FromResult<IEnumerable<SolicitudPermiso>>(solicitudes);
        }

        public Task<IEnumerable<PermisoDto>> MisPermisosAsync(Guid usuarioId)
        {
            var permisos = _context.PermisosUsuario
                .Where(p => p.UsuarioId == usuarioId)
                .Select(p => new PermisoDto
                {
                    Id = p.Id,
                    Permiso = p.Permiso,
                    ConcedidoEn = p.ConcedidoEn,
                    SolicitudId = p.SolicitudId
                })
                .ToList();
            return Task.FromResult<IEnumerable<PermisoDto>>(permisos);
        }

        public Task<IEnumerable<SolicitudPermiso>> PendientesAsync()
        {
            var pendientes = _context.SolicitudesPermiso
                .Where(s => s.Estado == EstadoSolicitudPermiso.Pendiente)
                .ToList();

            return Task.FromResult<IEnumerable<SolicitudPermiso>>(pendientes);
        }

        public async Task<SolicitudPermiso?> RechazarAsync(Guid solicitudId, Guid usuarioId, string motivo)
        {
            var solicitud = await _context.SolicitudesPermiso.FindAsync(solicitudId);

            if (solicitud == null)
            {
                return null;
            }

            solicitud.Rechazar(usuarioId, motivo, DateTime.UtcNow);
            await _context.SaveChangesAsync();
            return solicitud;
        }
    }
}
