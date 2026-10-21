using Core.Domain.Entities;
using Application.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities.Enums;

namespace Core.Application.Tests;

public class SolicitudPermisoServiceTests
{
    //Helper para tener los datos en memoria
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RF_GP_01_CrearAsync_PersisteSolicitudPendiente()
    {
        //Arrange
        //Crear usuario en la base de datos en memoria
        using var context = CreateContext();
        var usuario = new Usuario("carlos@dominio.com", "afafasdfefdsfdsf");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        var service = new SolicitudPermisoService(context);

        //Act
        var resultado = await service.CrearAsync(usuario.Id, "exportar-inventario", "Necesito permiso para salir temprano.");

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Pendiente, resultado.Estado);
        Assert.Equal("exportar-inventario", resultado.Permiso);
        Assert.Single(context.SolicitudesPermiso);
    }

    [Fact]
    public async Task RF_GP_02_MiasAsync_SoloDevuelveLasDelUsuario()
    {
        //Arrange
        using var context = CreateContext();
        var usuario1 = new Usuario("carlos@dominio.com", "afafasdfefdsfdsf");
        var usuario2 = new Usuario("maria@dominio.com", "ghjghjghjghj");
        context.Usuarios.AddRange(usuario1, usuario2);
        await context.SaveChangesAsync();
        var service = new SolicitudPermisoService(context);
        context.SolicitudesPermiso.Add(new SolicitudPermiso(usuario1.Id, "exportar-inventario", "Necesito permiso para salir temprano."));
        context.SolicitudesPermiso.Add(new SolicitudPermiso(usuario2.Id, "importar-inventario", "Necesito permiso para entrar tarde."));
        await context.SaveChangesAsync();

        //Act
        var resultado = await service.MiasAsync(usuario1.Id);

        //Assert
        Assert.Single(resultado);
        Assert.All(resultado, s => Assert.Equal(usuario1.Id, s.SolicitanteUsuarioId));
    }

    [Fact]
    public async Task RF_GP_02_PendientesAsync_SoloDevuelveLasPendientes()
    {
        //Arrange
        using var context = CreateContext();
        var usuario1 = new Usuario("carlos@dominio.com", "afafasdfefdsfdsf");
        var adminId = Guid.NewGuid();
        context.Usuarios.Add(usuario1);
        await context.SaveChangesAsync();
        var service = new SolicitudPermisoService(context);
        context.SolicitudesPermiso.Add(new SolicitudPermiso(usuario1.Id, "exportar-inventario", "Necesito permiso para salir temprano."));
        var rechazada = new SolicitudPermiso(usuario1.Id, "importar-inventario", "Necesito permiso para entrar tarde.");
        rechazada.Rechazar(adminId, "No cumple", DateTime.UtcNow);
        context.SolicitudesPermiso.Add(rechazada);
        await context.SaveChangesAsync();

        //Act
        var resultado = await service.PendientesAsync();

        //Assert
        Assert.Single(resultado);
        Assert.All(resultado, s => Assert.Equal(EstadoSolicitudPermiso.Pendiente, s.Estado));
    }

    [Fact]
    public async Task RF_GP_03_AprobarAsync_RegistraQuienYCuando()
    {
        //Arrange
        using var context = CreateContext();
        var solicitante = new Usuario("julio@test.com", "password123");
        var adminId = Guid.NewGuid();
        context.Usuarios.Add(solicitante);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context);

        //Act
        var resultado = await servicio.AprobarAsync(solicitud.Id, adminId);

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Aplicada, resultado.Estado);
        Assert.Equal(adminId, resultado.ResueltoPorUsuarioId);
        Assert.NotNull(resultado.FechaResolucion);

    }

    [Fact]
    public async Task RF_GP_04_RechazarAsync_GuardaELMotivo()
    {
        //Arrange
        using var context = CreateContext();
        var solicitante = new Usuario("julio@test.com", "password123");
        var adminId = Guid.NewGuid();
        context.Usuarios.Add(solicitante);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context);

        //Act
        var motivo = "No cumple con los requisitos.";
        var resultado = await servicio.RechazarAsync(solicitud.Id, adminId, motivo);

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Rechazada, resultado.Estado);
        Assert.Equal(adminId, resultado.ResueltoPorUsuarioId);
        Assert.Equal(motivo, resultado.MotivoRechazo);
    }

    [Fact]
    public async Task RF_GP_04_RechazarAsync_SinMotivo_LanzaExcepcion()
    {
        //Arrange
        using var context = CreateContext();
        var solicitante = new Usuario("julio@test.com", "password123");
        var adminId = Guid.NewGuid();
        context.Usuarios.Add(solicitante);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context);

        //Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RechazarAsync(solicitud.Id, adminId, ""));
        Assert.Equal("El motivo de rechazo es obligatorio.", exception.Message);
        Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
    }

    [Fact]
    public async Task RF_GP_05_AprobarAsync_ConcedePermiso()
    {
        //Arrange
        using var context = CreateContext();
        var adminId = Guid.NewGuid();
        var solicitante = new Usuario("felipe@test.com", "password123");
        context.Usuarios.Add(solicitante);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context);

        //Act
        var resultado = await servicio.AprobarAsync(solicitud.Id, adminId);

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Aplicada, resultado.Estado);
        Assert.Equal(adminId, resultado.ResueltoPorUsuarioId);
        var permiso = Assert.Single(context.PermisosUsuario);
        Assert.Equal(solicitante.Id, permiso.UsuarioId);
        Assert.Equal("exportar-inventario", permiso.Permiso);
    }


}