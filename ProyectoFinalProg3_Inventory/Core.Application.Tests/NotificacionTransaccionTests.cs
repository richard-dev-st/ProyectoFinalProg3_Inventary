using Application.Services;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

/// <summary>
/// Hallazgo 2: la notificación debe formar parte de la misma unidad de trabajo que el
/// evento de negocio. Si el guardado de la notificación falla, la solicitud no puede
/// quedar aprobada/creada sin avisar al usuario (RF-NOT-01/02).
/// </summary>
public class NotificacionTransaccionTests
{
    private static DbContextOptions<ApplicationDbContext> Opciones()
        => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static Usuario CrearUsuario(string email, RolUsuario rol = RolUsuario.Estandar)
    {
        var usuario = new Usuario(email, "hash-de-prueba");

        if (rol != RolUsuario.Estandar)
        {
            usuario.CambiarRol(rol, Guid.Empty);
        }

        usuario.ActivarCuenta(usuario.TokenActivacion!);
        return usuario;
    }

    // Pre-fix hay dos SaveChanges (solicitud y notificación): el segundo falla y la
    // solicitud queda creada sin notificación. Post-fix hay una sola unidad de trabajo.
    [Fact]
    public async Task RF_NOT_01_CrearSolicitud_GuardaEventoYNotificacionEnLaMismaOperacion()
    {
        await using var context = new ContextoConFalloDeGuardado(Opciones());
        var admin = CrearUsuario("admin@test.com", RolUsuario.Administrador);
        var estandar = CrearUsuario("estandar@test.com");
        context.Usuarios.AddRange(admin, estandar);
        await context.SaveChangesAsync();

        var solicitudes = new SolicitudPermisoService(context, new NotificacionService(context));

        context.FallarEnElGuardadoNumero(2);
        var solicitud = await solicitudes.CrearAsync(estandar.Id, "exportar-inventario", "Cierre de mes.");

        Assert.Equal(1, await context.SolicitudesPermiso.CountAsync());
        Assert.Equal(1, await context.Notificaciones.CountAsync(
            n => n.UsuarioId == admin.Id && n.Tipo == NotificacionService.TipoSolicitudCreada));
        Assert.Equal(1, await context.CorreosEnCola.CountAsync(c => c.Destinatario == admin.Email));
        Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
    }

    [Fact]
    public async Task RF_NOT_02_AprobarSolicitud_GuardaResolucionYNotificacionEnLaMismaOperacion()
    {
        await using var context = new ContextoConFalloDeGuardado(Opciones());
        var admin = CrearUsuario("admin@test.com", RolUsuario.Administrador);
        var estandar = CrearUsuario("estandar@test.com");
        context.Usuarios.AddRange(admin, estandar);
        await context.SaveChangesAsync();

        var solicitudes = new SolicitudPermisoService(context, new NotificacionService(context));
        var solicitud = await solicitudes.CrearAsync(estandar.Id, "exportar-inventario", "Cierre de mes.");
        context.ResetearContadorDeGuardados();

        context.FallarEnElGuardadoNumero(2);
        await solicitudes.AprobarAsync(solicitud.Id, admin.Id);

        Assert.Equal(EstadoSolicitudPermiso.Aplicada, solicitud.Estado);
        Assert.Equal(1, await context.PermisosUsuario.CountAsync(p => p.UsuarioId == estandar.Id));
        Assert.Equal(1, await context.Notificaciones.CountAsync(
            n => n.UsuarioId == estandar.Id && n.Tipo == NotificacionService.TipoSolicitudAprobada));
    }

    /// <summary>
    /// Contexto de prueba que cuenta los guardados y puede lanzar en el N-ésimo.
    /// Simula una caída de la base de datos entre el evento y la notificación.
    /// </summary>
    private sealed class ContextoConFalloDeGuardado : ApplicationDbContext
    {
        private int _guardados;
        private int? _fallarEn;

        public ContextoConFalloDeGuardado(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public void ResetearContadorDeGuardados() => _guardados = 0;

        public void FallarEnElGuardadoNumero(int numero)
        {
            _guardados = 0;
            _fallarEn = numero;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _guardados++;

            if (_fallarEn.HasValue && _guardados == _fallarEn.Value)
            {
                throw new InvalidOperationException(
                    $"Fallo simulado de base de datos en el guardado {_guardados}.");
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
