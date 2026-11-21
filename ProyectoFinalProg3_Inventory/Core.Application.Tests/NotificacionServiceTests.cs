using Application.DTOs.Divisa;
using Application.Interfaces;
using Application.Services;
using Business.Inventory.Entities;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class NotificacionServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Usuario CrearUsuario(string email, RolUsuario rol = RolUsuario.Estandar, bool activo = true)
    {
        var usuario = new Usuario(email, "hash-de-prueba");

        if (rol != RolUsuario.Estandar)
        {
            usuario.CambiarRol(rol, Guid.Empty);
        }

        if (activo)
        {
            usuario.ActivarCuenta(usuario.TokenActivacion!);
        }

        return usuario;
    }

    // Auditoría (caso no cubierto): sin ningún Administrador activo la solicitud se
    // guarda igual y no se crea ninguna notificación ni correo.
    [Fact]
    public async Task RF_NOT_01_SinAdministradoresActivos_GuardaSolicitudSinNotificar()
    {
        using var context = CreateContext();
        var adminInactivo = CrearUsuario("admin-inactivo@test.com", RolUsuario.Administrador, activo: false);
        var estandar = CrearUsuario("estandar-sin-admin@test.com");
        context.Usuarios.AddRange(adminInactivo, estandar);
        await context.SaveChangesAsync();

        var notificaciones = new NotificacionService(context);
        var solicitudes = new SolicitudPermisoService(context, notificaciones);

        var solicitud = await solicitudes.CrearAsync(estandar.Id, "exportar-inventario", "Sin administradores.");

        Assert.Equal(1, await context.SolicitudesPermiso.CountAsync());
        Assert.Equal(0, await context.Notificaciones.CountAsync());
        Assert.Equal(0, await context.CorreosEnCola.CountAsync());
        Assert.Equal(Core.Domain.Entities.Enums.EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
    }

    // RF-NOT-01: solo los Administradores activos reciben la notificación.
    [Fact]
    public async Task RF_NOT_01_CrearSolicitud_NotificaSoloAdministradoresActivos()
    {
        using var context = CreateContext();
        var admin1 = CrearUsuario("admin1@test.com", RolUsuario.Administrador);
        var admin2 = CrearUsuario("admin2@test.com", RolUsuario.Administrador);
        var adminInactivo = CrearUsuario("admin3@test.com", RolUsuario.Administrador, activo: false);
        var estandar = CrearUsuario("estandar@test.com");
        context.Usuarios.AddRange(admin1, admin2, adminInactivo, estandar);
        await context.SaveChangesAsync();

        var notificaciones = new NotificacionService(context);
        var solicitudes = new SolicitudPermisoService(context, notificaciones);

        await solicitudes.CrearAsync(estandar.Id, "exportar-inventario", "Necesito exportar el inventario.");

        Assert.Equal(1, await context.Notificaciones.CountAsync(n => n.UsuarioId == admin1.Id && !n.Leida));
        Assert.Equal(1, await context.Notificaciones.CountAsync(n => n.UsuarioId == admin2.Id && !n.Leida));
        Assert.Equal(0, await context.Notificaciones.CountAsync(n => n.UsuarioId == adminInactivo.Id));
        Assert.Equal(0, await context.Notificaciones.CountAsync(n => n.UsuarioId == estandar.Id));

        // RF-NOT-06: cada notificación de un usuario con correo activo encola un correo.
        Assert.Equal(2, await context.CorreosEnCola.CountAsync());
    }

    // RF-NOT-02: aprobar y rechazar notifican al solicitante; el rechazo lleva el motivo.
    [Fact]
    public async Task RF_NOT_02_ResolverSolicitud_NotificaAlSolicitanteConResultado()
    {
        using var context = CreateContext();
        var admin = CrearUsuario("admin@test.com", RolUsuario.Administrador);
        var estandar = CrearUsuario("estandar@test.com");
        context.Usuarios.AddRange(admin, estandar);
        await context.SaveChangesAsync();

        var notificaciones = new NotificacionService(context);
        var solicitudes = new SolicitudPermisoService(context, notificaciones);

        var aprobada = await solicitudes.CrearAsync(estandar.Id, "exportar-inventario", "Para el cierre.");
        await solicitudes.AprobarAsync(aprobada.Id, admin.Id);

        var notificacionAprobada = await context.Notificaciones.SingleAsync(
            n => n.UsuarioId == estandar.Id && n.Tipo == NotificacionService.TipoSolicitudAprobada);
        Assert.Contains("aprobada", notificacionAprobada.Mensaje);
        Assert.False(notificacionAprobada.Leida);

        var rechazada = await solicitudes.CrearAsync(estandar.Id, "recibir-orden", "Para recibir compras.");
        await solicitudes.RechazarAsync(rechazada.Id, admin.Id, "No hay cupo en el turno");

        var notificacionRechazada = await context.Notificaciones.SingleAsync(
            n => n.UsuarioId == estandar.Id && n.Tipo == NotificacionService.TipoSolicitudRechazada);
        Assert.Contains("rechazada", notificacionRechazada.Mensaje);
        Assert.Contains("No hay cupo en el turno", notificacionRechazada.Mensaje);
    }

    // RF-NOT-03: enviar una orden a aprobación notifica a los Administradores.
    [Fact]
    public async Task RF_NOT_03_EnviarOrdenAprobacion_NotificaAdministradores()
    {
        using var context = CreateContext();
        var admin = CrearUsuario("admin@test.com", RolUsuario.Administrador);
        context.Usuarios.Add(admin);
        await context.SaveChangesAsync();

        var orden = new OrdenCompra
        {
            Id = 42,
            ProveedorId = 1,
            Total = 150.00m,
            Detalles = new List<DetalleOrden>
            {
                new DetalleOrden { ProductoId = 1, Cantidad = 3, Precio = 50.00m }
            }
        };

        var notificaciones = new NotificacionService(context);
        var ordenes = new OrdenCompraService(new RepoFalso(orden), new TasaFalsa(59m), context, notificaciones);

        await ordenes.EnviarAprobacionAsync(42);

        var notificacion = await context.Notificaciones.SingleAsync(
            n => n.UsuarioId == admin.Id && n.Tipo == NotificacionService.TipoOrdenEnviadaAprobacion);
        Assert.Contains("#42", notificacion.Mensaje);
        Assert.Equal(EstadoOrden.PendienteAprobacion, orden.Estado);
    }

    // RF-NOT-04: cada usuario consulta solo lo suyo y puede filtrar las no leídas.
    [Fact]
    public async Task RF_NOT_04_ObtenerPropias_SoloDevuelveLasDelUsuario()
    {
        using var context = CreateContext();
        var usuario1 = CrearUsuario("u1@test.com");
        var usuario2 = CrearUsuario("u2@test.com");
        context.Usuarios.AddRange(usuario1, usuario2);
        await context.SaveChangesAsync();

        var notificaciones = new NotificacionService(context);
        var propiaLeida = await notificaciones.CrearAsync(usuario1.Id, "Mensaje leído", "Prueba");
        await notificaciones.CrearAsync(usuario1.Id, "Mensaje nuevo", "Prueba");
        var ajena = await notificaciones.CrearAsync(usuario2.Id, "Mensaje ajeno", "Prueba");
        await notificaciones.MarcarLeidaAsync(propiaLeida.Id, usuario1.Id);

        var todas = (await notificaciones.ObtenerPropiasAsync(usuario1.Id)).ToList();
        var noLeidas = (await notificaciones.ObtenerPropiasAsync(usuario1.Id, soloNoLeidas: true)).ToList();

        Assert.Equal(2, todas.Count);
        Assert.Single(noLeidas);
        Assert.DoesNotContain(todas, n => n.Id == ajena.Id);
    }

    // RF-NOT-05: marcar la notificación de otro se rechaza y no cambia su estado.
    [Fact]
    public async Task RF_NOT_05_MarcarAjena_SeRechazaYNoCambiaEstado()
    {
        using var context = CreateContext();
        var duenio = CrearUsuario("duenio@test.com");
        var intruso = CrearUsuario("intruso@test.com");
        context.Usuarios.AddRange(duenio, intruso);
        await context.SaveChangesAsync();

        var notificaciones = new NotificacionService(context);
        var notificacion = await notificaciones.CrearAsync(duenio.Id, "Privada", "Prueba");

        Assert.False(await notificaciones.MarcarLeidaAsync(notificacion.Id, intruso.Id));
        Assert.False(notificacion.Leida);

        Assert.True(await notificaciones.MarcarLeidaAsync(notificacion.Id, duenio.Id));
        Assert.True(await notificaciones.MarcarLeidaAsync(notificacion.Id, duenio.Id)); // idempotente
        Assert.True(notificacion.Leida);
    }

    // RF-NOT-06/07: el canal interno siempre existe; el correo depende de la preferencia.
    [Fact]
    public async Task RF_NOT_07_CorreoDesactivado_ConservaInternaYNoEncolaCorreo()
    {
        using var context = CreateContext();
        var admin = CrearUsuario("admin@test.com", RolUsuario.Administrador);
        context.Usuarios.Add(admin);
        await context.SaveChangesAsync();

        var notificaciones = new NotificacionService(context);

        await notificaciones.ActualizarPreferenciaCorreoAsync(admin.Id, false);
        Assert.False(await notificaciones.ObtenerPreferenciaCorreoAsync(admin.Id));

        await notificaciones.CrearAsync(admin.Id, "Mensaje interno", "Prueba");

        Assert.Equal(1, await context.Notificaciones.CountAsync(n => n.UsuarioId == admin.Id));
        Assert.Equal(0, await context.CorreosEnCola.CountAsync());

        await notificaciones.ActualizarPreferenciaCorreoAsync(admin.Id, true);
        await notificaciones.CrearAsync(admin.Id, "Mensaje con correo", "Prueba");

        Assert.Equal(1, await context.CorreosEnCola.CountAsync());
        Assert.Equal(admin.Email, (await context.CorreosEnCola.SingleAsync()).Destinatario);
    }

    private sealed class RepoFalso : IOrdenCompraRepository
    {
        private readonly OrdenCompra? _orden;

        public RepoFalso(OrdenCompra? orden) => _orden = orden;

        public Task<OrdenCompra> GetByIdAsync(int id) => Task.FromResult(_orden!);
        public Task AddAsync(OrdenCompra orden) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class TasaFalsa : ITasaCambioService
    {
        private readonly decimal _tasa;

        public TasaFalsa(decimal tasa) => _tasa = tasa;

        public Task<TasaCambioDto> ObtenerTasaAsync(string origen, string destino, CancellationToken ct = default)
            => Task.FromResult(new TasaCambioDto(_tasa, origen, destino, DateTime.UtcNow));
    }
}
