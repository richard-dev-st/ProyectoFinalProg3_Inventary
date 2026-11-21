using Application.Services;
using Business.Inventory.Entities;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class ReporteServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Usuario CrearUsuario(string email)
        => new(email, "hash-de-prueba");

    private static void FijarFecha(ApplicationDbContext context, SolicitudPermiso solicitud, DateTime fecha)
        => context.Entry(solicitud).Property(s => s.CreadoEn).CurrentValue = fecha;

    private static void FijarEstado(ApplicationDbContext context, SolicitudPermiso solicitud, EstadoSolicitudPermiso estado)
        => context.Entry(solicitud).Property(s => s.Estado).CurrentValue = estado;

    private static void FijarEstado(ApplicationDbContext context, OrdenCompra orden, EstadoOrden estado)
        => context.Entry(orden).Property(o => o.Estado).CurrentValue = estado;

    // RF-REP-01/03/05: solicitudes agrupadas por estado y filtradas por período.
    [Fact]
    public async Task RF_REP_01_Solicitudes_DevuelveGruposPorEstadoYNoElListado()
    {
        using var context = CreateContext();
        var usuario = CrearUsuario("usuario@test.com");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var pendiente = new SolicitudPermiso(usuario.Id, "permiso-1", "d1");
        var aprobada = new SolicitudPermiso(usuario.Id, "permiso-2", "d2");
        var rechazada = new SolicitudPermiso(usuario.Id, "permiso-3", "d3");
        context.SolicitudesPermiso.AddRange(pendiente, aprobada, rechazada);
        await context.SaveChangesAsync();

        FijarFecha(context, pendiente, new DateTime(2026, 1, 10));
        FijarFecha(context, aprobada, new DateTime(2026, 2, 15));
        FijarEstado(context, aprobada, EstadoSolicitudPermiso.Aprobada);
        FijarFecha(context, rechazada, new DateTime(2026, 6, 20));
        FijarEstado(context, rechazada, EstadoSolicitudPermiso.Rechazada);
        await context.SaveChangesAsync();

        var reporte = await new ReporteService(context).ObtenerSolicitudesAsync(
            usuario.Id, esAdministrador: true, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(3, reporte.Grupos.Count); // 3 registros resumidos en 3 estados
        Assert.Equal(3, reporte.Total);
        Assert.Contains(reporte.Grupos, g => g.Estado == "Pendiente" && g.Cantidad == 1);
        Assert.Contains(reporte.Grupos, g => g.Estado == "Aprobada" && g.Cantidad == 1);
        Assert.Contains(reporte.Grupos, g => g.Estado == "Rechazada" && g.Cantidad == 1);

        // RF-REP-05: un rango distinto cambia el resultado.
        var soloPrimerTrimestre = await new ReporteService(context).ObtenerSolicitudesAsync(
            usuario.Id, esAdministrador: true, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        Assert.Equal(2, soloPrimerTrimestre.Total);
        Assert.DoesNotContain(soloPrimerTrimestre.Grupos, g => g.Estado == "Rechazada");
    }

    // RF-REP-02/03: órdenes agrupadas por proveedor y estado con monto total.
    [Fact]
    public async Task RF_REP_02_Ordenes_AgrupaPorProveedorYEstadoConMontos()
    {
        using var context = CreateContext();
        var proveedor1 = new Proveedor { Nombre = "Proveedor Uno" };
        var proveedor2 = new Proveedor { Nombre = "Proveedor Dos" };
        context.Proveedores.AddRange(proveedor1, proveedor2);
        await context.SaveChangesAsync();

        var ordenes = new[]
        {
            NuevaOrden(proveedor1.Id, 100m, EstadoOrden.Aprobada, new DateTime(2026, 2, 1)),
            NuevaOrden(proveedor1.Id, 50m, EstadoOrden.Recibida, new DateTime(2026, 3, 1)),
            NuevaOrden(proveedor2.Id, 200m, EstadoOrden.Aprobada, new DateTime(2026, 4, 1))
        };

        foreach (var orden in ordenes)
        {
            context.OrdenesCompra.Add(orden);
            FijarEstado(context, orden, orden.Estado);
        }

        await context.SaveChangesAsync();

        var reporte = await new ReporteService(context).ObtenerOrdenesAsync(
            Guid.NewGuid(), esAdministrador: true, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(3, reporte.TotalOrdenes);
        Assert.Equal(350m, reporte.MontoTotalUsd);
        Assert.Equal(3, reporte.Grupos.Count);

        var grupoProveedor1Aprobada = Assert.Single(
            reporte.Grupos.Where(g => g.ProveedorId == proveedor1.Id && g.Estado == "Aprobada"));
        Assert.Equal(1, grupoProveedor1Aprobada.CantidadOrdenes);
        Assert.Equal(100m, grupoProveedor1Aprobada.MontoTotalUsd);

        var grupoProveedor2 = Assert.Single(
            reporte.Grupos.Where(g => g.ProveedorId == proveedor2.Id));
        Assert.Equal("Proveedor Dos", grupoProveedor2.Proveedor);
        Assert.Equal(200m, grupoProveedor2.MontoTotalUsd);
    }

    // RF-REP-04: un Estándar solo obtiene datos propios en ambos reportes.
    [Fact]
    public async Task RF_REP_04_Estandar_SoloVeSusPropiosDatos()
    {
        using var context = CreateContext();
        var usuario1 = CrearUsuario("u1@test.com");
        var usuario2 = CrearUsuario("u2@test.com");
        context.Usuarios.AddRange(usuario1, usuario2);

        var proveedor = new Proveedor { Nombre = "Proveedor" };
        context.Proveedores.Add(proveedor);
        await context.SaveChangesAsync();

        context.SolicitudesPermiso.Add(new SolicitudPermiso(usuario1.Id, "p1", "d1"));
        context.SolicitudesPermiso.Add(new SolicitudPermiso(usuario2.Id, "p2", "d2"));
        context.OrdenesCompra.Add(NuevaOrden(proveedor.Id, 10m, EstadoOrden.Aprobada, DateTime.UtcNow, usuario1.Id));
        context.OrdenesCompra.Add(NuevaOrden(proveedor.Id, 990m, EstadoOrden.Aprobada, DateTime.UtcNow, usuario2.Id));
        await context.SaveChangesAsync();

        var servicio = new ReporteService(context);

        var solicitudes = await servicio.ObtenerSolicitudesAsync(usuario1.Id, esAdministrador: false);
        var ordenes = await servicio.ObtenerOrdenesAsync(usuario1.Id, esAdministrador: false);

        Assert.Equal(1, solicitudes.Total);
        Assert.Equal(1, ordenes.TotalOrdenes);
        Assert.Equal(10m, ordenes.MontoTotalUsd);

        // Los datos nuevos del otro usuario no cambian los números de usuario1.
        context.SolicitudesPermiso.Add(new SolicitudPermiso(usuario2.Id, "p3", "d3"));
        context.OrdenesCompra.Add(NuevaOrden(proveedor.Id, 500m, EstadoOrden.Aprobada, DateTime.UtcNow, usuario2.Id));
        await context.SaveChangesAsync();

        var solicitudesDespues = await servicio.ObtenerSolicitudesAsync(usuario1.Id, esAdministrador: false);
        var ordenesDespues = await servicio.ObtenerOrdenesAsync(usuario1.Id, esAdministrador: false);

        Assert.Equal(1, solicitudesDespues.Total);
        Assert.Equal(10m, ordenesDespues.MontoTotalUsd);

        // El Administrador sí ve todo.
        var solicitudesAdmin = await servicio.ObtenerSolicitudesAsync(usuario1.Id, esAdministrador: true);
        Assert.Equal(3, solicitudesAdmin.Total);
    }

    // RF-REP-05: los extremos del rango son inclusivos.
    [Fact]
    public async Task RF_REP_05_Rango_IncluyeLosExtremos()
    {
        using var context = CreateContext();
        var usuario = CrearUsuario("u@test.com");
        context.Usuarios.Add(usuario);

        var enElLimiteInferior = new SolicitudPermiso(usuario.Id, "p1", "d1");
        var enElLimiteSuperior = new SolicitudPermiso(usuario.Id, "p2", "d2");
        var fuera = new SolicitudPermiso(usuario.Id, "p3", "d3");
        context.SolicitudesPermiso.AddRange(enElLimiteInferior, enElLimiteSuperior, fuera);
        await context.SaveChangesAsync();

        FijarFecha(context, enElLimiteInferior, new DateTime(2026, 1, 1));
        FijarFecha(context, enElLimiteSuperior, new DateTime(2026, 3, 31));
        FijarFecha(context, fuera, new DateTime(2026, 4, 1));
        await context.SaveChangesAsync();

        var reporte = await new ReporteService(context).ObtenerSolicitudesAsync(
            usuario.Id, esAdministrador: true, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        Assert.Equal(2, reporte.Total);
    }

    // Hallazgo 1: `hasta=9999-12-31` (el último día representable) desborda al sumar un día.
    [Fact]
    public async Task RF_REP_05_HastaFechaMaxima_NoDesborda()
    {
        using var context = CreateContext();
        var usuario = CrearUsuario("maxima@test.com");
        var proveedor = new Proveedor { Nombre = "Proveedor Máximo" };
        context.Usuarios.Add(usuario);
        context.Proveedores.Add(proveedor);
        await context.SaveChangesAsync();

        var solicitud = new SolicitudPermiso(usuario.Id, "p1", "d1");
        context.SolicitudesPermiso.Add(solicitud);
        var orden = NuevaOrden(proveedor.Id, 25m, EstadoOrden.Aprobada, new DateTime(9999, 12, 31, 12, 0, 0));
        context.OrdenesCompra.Add(orden);
        await context.SaveChangesAsync();

        var fechaMaxima = new DateTime(9999, 12, 31);
        var servicio = new ReporteService(context);

        var solicitudes = await servicio.ObtenerSolicitudesAsync(
            usuario.Id, esAdministrador: true, new DateTime(2026, 1, 1), fechaMaxima);
        var ordenes = await servicio.ObtenerOrdenesAsync(
            usuario.Id, esAdministrador: true, new DateTime(2026, 1, 1), fechaMaxima);

        Assert.Equal(1, solicitudes.Total);
        Assert.Equal(1, ordenes.TotalOrdenes);
        Assert.Equal(25m, ordenes.MontoTotalUsd);
    }

    // RF-REP-05: `hasta=AAAA-MM-DD` incluye todo ese día, no solo la medianoche.
    [Fact]
    public async Task RF_REP_05_HastaFecha_IncluyeTodoElDia()
    {
        using var context = CreateContext();
        var usuario = CrearUsuario("dia@test.com");
        var proveedor = new Proveedor { Nombre = "Proveedor Día" };
        context.Usuarios.Add(usuario);
        context.Proveedores.Add(proveedor);
        await context.SaveChangesAsync();

        var madrugada = new SolicitudPermiso(usuario.Id, "p1", "d1");
        var noche = new SolicitudPermiso(usuario.Id, "p2", "d2");
        var siguienteDia = new SolicitudPermiso(usuario.Id, "p3", "d3");
        context.SolicitudesPermiso.AddRange(madrugada, noche, siguienteDia);

        var ordenTemprano = NuevaOrden(proveedor.Id, 10m, EstadoOrden.Aprobada, new DateTime(2026, 12, 31, 0, 0, 0));
        var ordenTarde = NuevaOrden(proveedor.Id, 20m, EstadoOrden.Aprobada, new DateTime(2026, 12, 31, 23, 59, 0));
        var ordenSiguiente = NuevaOrden(proveedor.Id, 99m, EstadoOrden.Aprobada, new DateTime(2027, 1, 1));
        context.OrdenesCompra.AddRange(ordenTemprano, ordenTarde, ordenSiguiente);
        await context.SaveChangesAsync();

        FijarFecha(context, madrugada, new DateTime(2026, 12, 31, 0, 0, 0));
        FijarFecha(context, noche, new DateTime(2026, 12, 31, 23, 59, 0));
        FijarFecha(context, siguienteDia, new DateTime(2027, 1, 1));
        await context.SaveChangesAsync();

        var servicio = new ReporteService(context);

        var solicitudes = await servicio.ObtenerSolicitudesAsync(
            usuario.Id, esAdministrador: true, new DateTime(2026, 12, 1), new DateTime(2026, 12, 31));
        var ordenes = await servicio.ObtenerOrdenesAsync(
            usuario.Id, esAdministrador: true, new DateTime(2026, 12, 1), new DateTime(2026, 12, 31));

        Assert.Equal(2, solicitudes.Total);
        Assert.Equal(2, ordenes.TotalOrdenes);
        Assert.Equal(30m, ordenes.MontoTotalUsd);
    }

    [Fact]
    public async Task RF_REP_05_RangoInvertido_LanzaInvalidOperation()
    {
        using var context = CreateContext();

        var servicio = new ReporteService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ObtenerSolicitudesAsync(Guid.NewGuid(), true, new DateTime(2026, 12, 31), new DateTime(2026, 1, 1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ObtenerOrdenesAsync(Guid.NewGuid(), true, new DateTime(2026, 12, 31), new DateTime(2026, 1, 1)));
    }

    private static OrdenCompra NuevaOrden(
        int proveedorId,
        decimal total,
        EstadoOrden estado,
        DateTime fecha,
        Guid? creadoPor = null)
    {
        var orden = new OrdenCompra
        {
            ProveedorId = proveedorId,
            Total = total,
            FechaCreacion = fecha,
            CreadoPorUsuarioId = creadoPor ?? Guid.NewGuid()
        };

        orden.Detalles.Add(new DetalleOrden { ProductoId = 1, Cantidad = 1, Precio = total });

        // El estado se fija con la máquina de estados para no escribir el setter privado.
        if (estado == EstadoOrden.Aprobada)
        {
            orden.EnviarAAprobacion();
            orden.Aprobar();
        }
        else if (estado == EstadoOrden.Recibida)
        {
            orden.EnviarAAprobacion();
            orden.Aprobar();
            orden.Recibir();
        }
        else if (estado == EstadoOrden.PendienteAprobacion)
        {
            orden.EnviarAAprobacion();
        }

        return orden;
    }
}
