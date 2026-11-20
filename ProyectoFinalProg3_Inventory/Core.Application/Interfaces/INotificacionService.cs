using Application.DTOs.Notificaciones;
using Core.Domain.Entities;

namespace Application.Interfaces
{
    public interface INotificacionService
    {
        /// <summary>
        /// Crea la notificación interna del usuario y, si tiene el correo activo,
        /// encola el correo correspondiente (RF-NOT-06/07).
        /// </summary>
        Task<Notificacion> CrearAsync(Guid usuarioId, string mensaje, string tipo, string? referenciaId = null);

        /// <summary>RF-NOT-01: notifica a todos los Administradores activos.</summary>
        Task<int> NotificarAdministradoresAsync(string mensaje, string tipo, string? referenciaId = null);

        /// <summary>RF-NOT-04: solo las notificaciones del usuario indicado.</summary>
        Task<IEnumerable<NotificacionDto>> ObtenerPropiasAsync(Guid usuarioId, bool soloNoLeidas = false);

        /// <summary>
        /// RF-NOT-05: devuelve false si la notificación no existe o pertenece a otro usuario.
        /// </summary>
        Task<bool> MarcarLeidaAsync(Guid notificacionId, Guid usuarioId);

        Task<bool> ObtenerPreferenciaCorreoAsync(Guid usuarioId);

        Task ActualizarPreferenciaCorreoAsync(Guid usuarioId, bool recibeCorreos);
    }
}
