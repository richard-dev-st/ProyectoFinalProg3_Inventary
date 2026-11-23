using Application.DTOs.Permisos;
using Application.Interfaces;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Application.Services
{
    public class SolicitudPermisoService : ISolicitudPermisoService
    {
        private readonly IApplicationDbContext _context;
        private readonly INotificacionService _notificaciones;
        private readonly IAuditoriaService _auditoria;

        public SolicitudPermisoService(
            IApplicationDbContext context,
            INotificacionService notificaciones,
            IAuditoriaService auditoria)
        {
            _context = context;
            _notificaciones = notificaciones;
            _auditoria = auditoria;
        }

        public async Task<SolicitudPermiso?> AprobarAsync(Guid solicitudId, Guid usuarioId)
        {
            var s = await _context.SolicitudesPermiso.FindAsync(solicitudId);

            if (s == null)
            {
                return null;
            }

            // RF-AUD-02: el actor es el administrador que resuelve.
            var admin = await _context.Usuarios.FindAsync(usuarioId)
                ?? throw new UnauthorizedAccessException("Usuario no válido.");

            // RF-AUD-02: valor anterior antes de resolver.
            var valorAnterior = JsonSerializer.Serialize(new
            {
                Estado = s.Estado.ToString(),
                ResueltoPorUsuarioId = s.ResueltoPorUsuarioId
            });

            var fecha = DateTime.UtcNow;
            s.Aprobar(usuarioId, fecha);

            // RF-GP-05: aplicar el cambio de permiso automaticamente en la misma transaccion
            _context.PermisosUsuario.Add(new PermisoUsuario(s.SolicitanteUsuarioId, s.Permiso, s.Id, fecha));

            var valorNuevo = JsonSerializer.Serialize(new
            {
                Estado = s.Estado.ToString(),
                ResueltoPorUsuarioId = s.ResueltoPorUsuarioId,
                s.FechaResolucion
            });

            _context.Auditoria.Add(_auditoria.Preparar(
                admin.Id,
                admin.Email,
                AccionesAuditoria.Aprobar,
                EntidadesAuditoria.SolicitudPermiso,
                s.Id.ToString(),
                valorAnterior,
                valorNuevo,
                fecha));

            // RF-NOT-02: el solicitante sabe que fue aprobada. Hallazgo 2: se prepara en el
            // mismo contexto y se guarda junto con la resolución (una sola unidad de trabajo).
            await _notificaciones.PrepararAsync(
                s.SolicitanteUsuarioId,
                $"Tu solicitud de permiso '{s.Permiso}' fue aprobada.",
                NotificacionService.TipoSolicitudAprobada,
                s.Id.ToString());

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

            // RF-NOT-01: todos los Administradores activos, nadie más. Hallazgo 2: la
            // notificación se prepara antes del único SaveChanges del caso de uso.
            await _notificaciones.PrepararAdministradoresAsync(
                $"{usuario.Email} creó la solicitud de permiso '{permiso}'.",
                NotificacionService.TipoSolicitudCreada,
                solicitud.Id.ToString());

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

            // RF-AUD-02: el actor es el administrador que resuelve.
            var admin = await _context.Usuarios.FindAsync(usuarioId)
                ?? throw new UnauthorizedAccessException("Usuario no válido.");

            // RF-AUD-02: valor anterior antes de resolver.
            var valorAnterior = JsonSerializer.Serialize(new
            {
                Estado = solicitud.Estado.ToString(),
                ResueltoPorUsuarioId = solicitud.ResueltoPorUsuarioId
            });

            var fecha = DateTime.UtcNow;
            solicitud.Rechazar(usuarioId, motivo, fecha);

            var valorNuevo = JsonSerializer.Serialize(new
            {
                Estado = solicitud.Estado.ToString(),
                ResueltoPorUsuarioId = solicitud.ResueltoPorUsuarioId,
                solicitud.MotivoRechazo,
                solicitud.FechaResolucion
            });

            _context.Auditoria.Add(_auditoria.Preparar(
                admin.Id,
                admin.Email,
                AccionesAuditoria.Rechazar,
                EntidadesAuditoria.SolicitudPermiso,
                solicitud.Id.ToString(),
                valorAnterior,
                valorNuevo,
                fecha));

            // RF-NOT-02: el solicitante sabe que fue rechazada y por qué. Hallazgo 2:
            // rechazo y notificación se guardan juntos.
            await _notificaciones.PrepararAsync(
                solicitud.SolicitanteUsuarioId,
                $"Tu solicitud de permiso '{solicitud.Permiso}' fue rechazada. Motivo: {motivo}.",
                NotificacionService.TipoSolicitudRechazada,
                solicitud.Id.ToString());

            await _context.SaveChangesAsync();

            return solicitud;
        }
    }
}
