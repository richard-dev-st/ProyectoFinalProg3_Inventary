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
        var service = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        //Act
        var resultado = await service.CrearAsync(usuario.Id, "exportar-inventario", "Necesito permiso para salir temprano.");

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Pendiente, resultado.Estado);
        Assert.Equal("exportar-inventario", resultado.Permiso);
        Assert.Single(context.SolicitudesPermiso);
    }

    // Auditoría (caso no cubierto): una entrada vacía se rechaza antes de persistir.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RF_GP_01_PermisoVacio_SeRechaza(string permisoVacio)
    {
        using var context = CreateContext();
        var usuario = new Usuario("vacio@dominio.com", "afafasdfefdsfdsf");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        var service = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearAsync(usuario.Id, permisoVacio, "Descripción válida."));
        Assert.Empty(context.SolicitudesPermiso);
    }

    // Hallazgo 3: el permiso no puede superar el tamaño de la columna (nvarchar(100)).
    [Fact]
    public async Task RF_GP_01_PermisoFueraDeRango_SeRechaza()
    {
        using var context = CreateContext();
        var usuario = new Usuario("limite@dominio.com", "afafasdfefdsfdsf");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        var service = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        var permisoLargo = new string('x', 101);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearAsync(usuario.Id, permisoLargo, "Descripción válida."));
        Assert.Empty(context.SolicitudesPermiso);
    }

    // Hallazgo 3: la descripción no puede superar el tamaño de la columna (nvarchar(500)).
    [Fact]
    public async Task RF_GP_01_DescripcionFueraDeRango_SeRechaza()
    {
        using var context = CreateContext();
        var usuario = new Usuario("limite-descripcion@dominio.com", "afafasdfefdsfdsf");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        var service = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        var descripcionLarga = new string('d', 501);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearAsync(usuario.Id, "exportar-inventario", descripcionLarga));
        Assert.Empty(context.SolicitudesPermiso);
    }

    // Hallazgo 3: el motivo de rechazo no puede superar el tamaño de la columna (nvarchar(500)).
    [Fact]
    public async Task RF_GP_04_MotivoFueraDeRango_SeRechaza()
    {
        using var context = CreateContext();
        var solicitante = new Usuario("limite-motivo@dominio.com", "afafasdfefdsfdsf");
        var admin = new Usuario("admin-limite-motivo@dominio.com", "afafasdfefdsfdsf");
        context.Usuarios.AddRange(solicitante, admin);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Descripción válida.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        var motivoLargo = new string('m', 501);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.RechazarAsync(solicitud.Id, admin.Id, motivoLargo));
        Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
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
        var service = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));
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
        var service = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));
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
        var admin = new Usuario("admin@test.com", "password123");
        context.Usuarios.AddRange(solicitante, admin);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        //Act
        var resultado = await servicio.AprobarAsync(solicitud.Id, admin.Id);

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Aplicada, resultado.Estado);
        Assert.Equal(admin.Id, resultado.ResueltoPorUsuarioId);
        Assert.NotNull(resultado.FechaResolucion);

    }

    [Fact]
    public async Task RF_GP_04_RechazarAsync_GuardaELMotivo()
    {
        //Arrange
        using var context = CreateContext();
        var solicitante = new Usuario("julio@test.com", "password123");
        var admin = new Usuario("admin-julio@test.com", "password123");
        context.Usuarios.AddRange(solicitante, admin);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        //Act
        var motivo = "No cumple con los requisitos.";
        var resultado = await servicio.RechazarAsync(solicitud.Id, admin.Id, motivo);

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Rechazada, resultado.Estado);
        Assert.Equal(admin.Id, resultado.ResueltoPorUsuarioId);
        Assert.Equal(motivo, resultado.MotivoRechazo);
    }

    [Fact]
    public async Task RF_GP_04_RechazarAsync_SinMotivo_LanzaExcepcion()
    {
        //Arrange
        using var context = CreateContext();
        var solicitante = new Usuario("julio@test.com", "password123");
        var admin = new Usuario("admin-sinmotivo@test.com", "password123");
        context.Usuarios.AddRange(solicitante, admin);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        //Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RechazarAsync(solicitud.Id, admin.Id, ""));
        Assert.Equal("El motivo de rechazo es obligatorio.", exception.Message);
        Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
    }

    [Fact]
    public async Task RF_GP_05_AprobarAsync_ConcedePermiso()
    {
        //Arrange
        using var context = CreateContext();
        var solicitante = new Usuario("felipe@test.com", "password123");
        var admin = new Usuario("admin-felipe@test.com", "password123");
        context.Usuarios.AddRange(solicitante, admin);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Necesito permiso para salir temprano.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        //Act
        var resultado = await servicio.AprobarAsync(solicitud.Id, admin.Id);

        //Assert
        Assert.NotNull(resultado);
        Assert.Equal(EstadoSolicitudPermiso.Aplicada, resultado.Estado);
        Assert.Equal(admin.Id, resultado.ResueltoPorUsuarioId);
        var permiso = Assert.Single(context.PermisosUsuario);
        Assert.Equal(solicitante.Id, permiso.UsuarioId);
        Assert.Equal("exportar-inventario", permiso.Permiso);
    }

    // RF-AUD-02: aprobar una solicitud deja rastro del admin, la entidad y el cambio de estado.
    [Fact]
    public async Task RF_AUD_02_Aprobar_RegistraAuditoria()
    {
        using var context = CreateContext();
        var solicitante = new Usuario("aud-aprobar@test.com", "password123");
        var admin = new Usuario("admin-aud-aprobar@test.com", "password123");
        context.Usuarios.AddRange(solicitante, admin);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Descripción válida.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        await servicio.AprobarAsync(solicitud.Id, admin.Id);

        var registro = Assert.Single(context.Auditoria);
        Assert.Equal(admin.Id, registro.UsuarioId);
        Assert.Equal(admin.Email, registro.UsuarioEmail);
        Assert.Equal("Aprobar", registro.Accion);
        Assert.Equal("SolicitudPermiso", registro.Entidad);
        Assert.Equal(solicitud.Id.ToString(), registro.EntidadId);
        Assert.Contains("Pendiente", registro.ValorAnterior);
        Assert.Contains("Aplicada", registro.ValorNuevo);
        Assert.True(registro.FechaHoraUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    // RF-AUD-02: rechazar deja rastro con el motivo, sin datos del solicitante de más.
    [Fact]
    public async Task RF_AUD_02_Rechazar_RegistraAuditoria()
    {
        using var context = CreateContext();
        var solicitante = new Usuario("aud-rechazar@test.com", "password123");
        var admin = new Usuario("admin-aud-rechazar@test.com", "password123");
        context.Usuarios.AddRange(solicitante, admin);
        var solicitud = new SolicitudPermiso(solicitante.Id, "exportar-inventario", "Descripción válida.");
        context.SolicitudesPermiso.Add(solicitud);
        await context.SaveChangesAsync();
        var servicio = new SolicitudPermisoService(context, new NotificacionService(context), new AuditoriaService(context));

        var motivo = "No cumple con los requisitos.";
        await servicio.RechazarAsync(solicitud.Id, admin.Id, motivo);

        var registro = Assert.Single(context.Auditoria);
        Assert.Equal(admin.Id, registro.UsuarioId);
        Assert.Equal("Rechazar", registro.Accion);
        Assert.Equal("SolicitudPermiso", registro.Entidad);
        Assert.Equal(solicitud.Id.ToString(), registro.EntidadId);
        Assert.Contains("Pendiente", registro.ValorAnterior);
        Assert.Contains("Rechazada", registro.ValorNuevo);
        Assert.Contains(motivo, registro.ValorNuevo);
    }

}