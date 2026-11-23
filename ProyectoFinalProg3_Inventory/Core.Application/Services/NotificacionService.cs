using Application.DTOs.Notificaciones;
using Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class NotificacionService : INotificacionService
    {
        // Tipos de notificación usados por los eventos de la Práctica 5.
        public const string TipoSolicitudCreada = "SolicitudPermisoCreada";
        public const string TipoSolicitudAprobada = "SolicitudPermisoAprobada";
        public const string TipoSolicitudRechazada = "SolicitudPermisoRechazada";
        public const string TipoOrdenEnviadaAprobacion = "OrdenEnviadaAprobacion";

        private readonly IApplicationDbContext _context;

        public NotificacionService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notificacion> CrearAsync(Guid usuarioId, string mensaje, string tipo, string? referenciaId = null)
        {
            var notificacion = await PrepararAsync(usuarioId, mensaje, tipo, referenciaId);
            await _context.SaveChangesAsync();
            return notificacion;
        }

        /// <summary>
        /// Hallazgo 2: deja la notificación (y su correo) listos en el contexto. No guarda:
        /// el dueño de la operación decide cuándo cerrar la unidad de trabajo.
        /// </summary>
        public async Task<Notificacion> PrepararAsync(Guid usuarioId, string mensaje, string tipo, string? referenciaId = null)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                throw new InvalidOperationException("Usuario no encontrado");
            }

            var notificacion = new Notificacion(usuarioId, mensaje, tipo, referenciaId);
            _context.Notificaciones.Add(notificacion);

            // RF-NOT-06/07: el canal interno siempre; el correo solo si está habilitado.
            EncolarCorreoSiCorresponde(usuario, mensaje, tipo);

            return notificacion;
        }

        public async Task<int> NotificarAdministradoresAsync(string mensaje, string tipo, string? referenciaId = null)
        {
            var cantidad = await PrepararAdministradoresAsync(mensaje, tipo, referenciaId);

            if (cantidad > 0)
            {
                await _context.SaveChangesAsync();
            }

            return cantidad;
        }

        /// <summary>
        /// Hallazgo 2: prepara una notificación por Administrador activo sin guardar.
        /// </summary>
        public async Task<int> PrepararAdministradoresAsync(string mensaje, string tipo, string? referenciaId = null)
        {
            // RF-NOT-01: todos los Administradores activos, nadie más.
            var administradores = await _context.Usuarios
                .Where(u => u.Rol == RolUsuario.Administrador && u.Activo)
                .ToListAsync();

            foreach (var administrador in administradores)
            {
                _context.Notificaciones.Add(new Notificacion(administrador.Id, mensaje, tipo, referenciaId));
                EncolarCorreoSiCorresponde(administrador, mensaje, tipo);
            }

            return administradores.Count;
        }

        public async Task<IEnumerable<NotificacionDto>> ObtenerPropiasAsync(Guid usuarioId, bool soloNoLeidas = false)
        {
            var consulta = _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId);

            if (soloNoLeidas)
            {
                consulta = consulta.Where(n => !n.Leida);
            }

            return await consulta
                .OrderByDescending(n => n.FechaCreacion)
                .Select(n => new NotificacionDto
                {
                    Id = n.Id,
                    Mensaje = n.Mensaje,
                    Tipo = n.Tipo,
                    Leida = n.Leida,
                    FechaCreacion = n.FechaCreacion,
                    ReferenciaId = n.ReferenciaId
                })
                .ToListAsync();
        }

        public async Task<bool> MarcarLeidaAsync(Guid notificacionId, Guid usuarioId)
        {
            var notificacion = await _context.Notificaciones.FindAsync(notificacionId);

            // RF-NOT-05: no se puede marcar la notificación de otro usuario.
            if (notificacion == null || notificacion.UsuarioId != usuarioId)
            {
                return false;
            }

            notificacion.MarcarLeida();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ObtenerPreferenciaCorreoAsync(Guid usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                throw new InvalidOperationException("Usuario no encontrado");
            }

            return usuario.RecibeCorreos;
        }

        public async Task ActualizarPreferenciaCorreoAsync(Guid usuarioId, bool recibeCorreos)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                throw new InvalidOperationException("Usuario no encontrado");
            }

            usuario.CambiarPreferenciaCorreo(recibeCorreos);
            await _context.SaveChangesAsync();
        }

        private void EncolarCorreoSiCorresponde(Usuario usuario, string mensaje, string tipo)
        {
            if (!usuario.RecibeCorreos)
            {
                return;
            }

            var asunto = AsuntoPorTipo(tipo);
            _context.CorreosEnCola.Add(new CorreoEnCola(usuario.Email, asunto, $"<p>{mensaje}</p>"));
        }

        private static string AsuntoPorTipo(string tipo) => tipo switch
        {
            TipoSolicitudCreada => "Nueva solicitud de permiso",
            TipoSolicitudAprobada => "Solicitud de permiso aprobada",
            TipoSolicitudRechazada => "Solicitud de permiso rechazada",
            TipoOrdenEnviadaAprobacion => "Orden de compra enviada a aprobación",
            _ => "Notificación del sistema de inventario"
        };
    }
}
