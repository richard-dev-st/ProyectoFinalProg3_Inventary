using Application.DTOs.Divisa;
using Application.DTOs.Ordenes;
using Application.Exceptions;
using Application.Interfaces;
using Application.Services;
using Business.Inventory.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class OrdenCompraServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
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
        private readonly Exception? _error;

        public TasaFalsa(decimal tasa, Exception? error = null)
        {
            _tasa = tasa;
            _error = error;
        }

        public Task<TasaCambioDto> ObtenerTasaAsync(
            string origen,
            string destino,
            CancellationToken ct = default)
            => _error is null
                ? Task.FromResult(new TasaCambioDto(_tasa, origen, destino, DateTime.UtcNow))
                : Task.FromException<TasaCambioDto>(_error);
    }

    [Fact]
    public async Task OrdenInexistente_LanzaInvalidOperation()
    {
        using var context = CreateContext();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ObtenerTotalEnDopAsync(1));
    }

    [Fact]
    public async Task OrdenExistente_ConvierteYRedondea()
    {
        using var context = CreateContext();

        var orden = new OrdenCompra { Id = 7, Total = 10.01m };

        var service = new OrdenCompraService(
            new RepoFalso(orden),
            new TasaFalsa(59.427m),
            context,
            new NotificacionService(context));

        var resultado = await service.ObtenerTotalEnDopAsync(7);

        Assert.Equal(7, resultado.OrdenId);
        Assert.Equal(10.01m, resultado.TotalUsd);
        Assert.Equal(59.427m, resultado.Tasa);
        Assert.Equal(594.86m, resultado.TotalDop); // 10.01 * 59.427 = 594.86427
    }

    [Fact]
    public async Task ErrorExterno_SePropaga()
    {
        using var context = CreateContext();

        var orden = new OrdenCompra { Id = 7, Total = 100m };
        var error = new ServicioExternoException(
            "La clave de la API externa no está configurada.",
            ServicioExternoException.Causa.NoConfigurado);

        var service = new OrdenCompraService(
            new RepoFalso(orden),
            new TasaFalsa(0m, error),
            context,
            new NotificacionService(context));

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => service.ObtenerTotalEnDopAsync(7));

        Assert.Equal(ServicioExternoException.Causa.NoConfigurado, ex.Motivo);
    }

    [Fact]
    public async Task CrearBorrador_ProveedorInexistente_LanzaMensajeClaro()
    {
        // Preparar: catálogo vacío, el proveedor no existe.
        using var context = CreateContext();
        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        var dto = new CrearOrdenCompraDto
        {
            ProveedorId = 99,
            ordenes = new()
            {
                new() { ProductoId = 1, Cantidad = 1, Precio = 10m }
            }
        };

        // Actuar y verificar: RD-07 devuelve un mensaje claro, no el error genérico de EF.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearBorradorAsync(dto, Guid.NewGuid()));

        Assert.Contains("99", ex.Message);
        Assert.Contains("no existe", ex.Message);
    }

    [Fact]
    public async Task CrearBorrador_ProductoInexistente_LanzaMensajeClaro()
    {
        // Preparar: existe el proveedor pero no el producto.
        using var context = CreateContext();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Proveedor" });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        var dto = new CrearOrdenCompraDto
        {
            ProveedorId = 1,
            ordenes = new()
            {
                new() { ProductoId = 77, Cantidad = 1, Precio = 10m }
            }
        };

        // Actuar y verificar.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearBorradorAsync(dto, Guid.NewGuid()));

        Assert.Contains("77", ex.Message);
        Assert.Contains("no existe", ex.Message);
    }

    [Fact]
    public async Task CrearBorrador_ProductoDeOtroProveedor_LanzaMensajeClaro()
    {
        // Preparar: el producto 10 pertenece al proveedor 2, pero la orden es del 1.
        using var context = CreateContext();
        context.Proveedores.AddRange(
            new Proveedor { Id = 1, Nombre = "Carlos" },
            new Proveedor { Id = 2, Nombre = "Otro proveedor" });
        context.Productos.Add(new Producto
        {
            Id = 10,
            Nombre = "NVIDIA",
            Codigo = "GPU-01",
            ProveedorId = 2
        });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        var dto = new CrearOrdenCompraDto
        {
            ProveedorId = 1,
            ordenes = new()
            {
                new() { ProductoId = 10, Cantidad = 1, Precio = 100m }
            }
        };

        // Actuar y verificar: RF-NEG-02 impide mezclar productos de otro proveedor.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearBorradorAsync(dto, Guid.NewGuid()));

        Assert.Contains("no pertenece", ex.Message);
        Assert.Equal(0, await context.OrdenesCompra.CountAsync());
    }

    [Fact]
    public async Task ObtenerDetalleAsync_DevuelveLineasYProveedor()
    {
        // Preparar: orden con una línea y su producto/proveedor.
        using var context = CreateContext();
        var usuario = Guid.NewGuid();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Carlos" });
        context.Productos.Add(new Producto
        {
            Id = 10,
            Nombre = "NVIDIA",
            Codigo = "GPU-01",
            ProveedorId = 1
        });
        context.OrdenesCompra.Add(new OrdenCompra
        {
            Id = 5,
            ProveedorId = 1,
            CreadoPorUsuarioId = usuario,
            Total = 200m,
            Detalles = new() { new DetalleOrden { ProductoId = 10, Cantidad = 2, Precio = 100m } }
        });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null), new TasaFalsa(59m), context, new NotificacionService(context));

        // Actuar
        var detalle = await service.ObtenerDetalleAsync(5, usuario, esAdministrador: false);

        // Verificar
        Assert.NotNull(detalle);
        Assert.Equal("Carlos", detalle!.ProveedorNombre);
        var linea = Assert.Single(detalle.Detalles);
        Assert.Equal("NVIDIA", linea.ProductoNombre);
        Assert.Equal("GPU-01", linea.ProductoCodigo);
        Assert.Equal(200m, linea.Subtotal);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_OtroUsuario_LanzaUnauthorized()
    {
        // Preparar: la orden pertenece a otro usuario.
        using var context = CreateContext();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Carlos" });
        context.Productos.Add(new Producto { Id = 10, Nombre = "NVIDIA", ProveedorId = 1 });
        context.OrdenesCompra.Add(new OrdenCompra
        {
            Id = 5,
            ProveedorId = 1,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new() { new DetalleOrden { ProductoId = 10, Cantidad = 1, Precio = 10m } }
        });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null), new TasaFalsa(59m), context, new NotificacionService(context));

        // Actuar y verificar
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ObtenerDetalleAsync(5, Guid.NewGuid(), esAdministrador: false));
    }

    [Fact]
    public async Task ActualizarBorradorAsync_ReemplazaLineasYRecalculaTotal()
    {
        // Preparar: borrador con proveedor 1 y producto 10.
        using var context = CreateContext();
        var usuario = Guid.NewGuid();
        context.Proveedores.AddRange(
            new Proveedor { Id = 1, Nombre = "Carlos" },
            new Proveedor { Id = 2, Nombre = "Otro" });
        context.Productos.AddRange(
            new Producto { Id = 10, Nombre = "NVIDIA", Codigo = "GPU-01", Precio = 100m, ProveedorId = 1 },
            new Producto { Id = 20, Nombre = "Monitor", Codigo = "MON-01", Precio = 50m, ProveedorId = 2 });
        context.OrdenesCompra.Add(new OrdenCompra
        {
            Id = 5,
            ProveedorId = 1,
            CreadoPorUsuarioId = usuario,
            Detalles = new() { new DetalleOrden { ProductoId = 10, Cantidad = 1, Precio = 100m } }
        });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null), new TasaFalsa(59m), context, new NotificacionService(context));

        var dto = new CrearOrdenCompraDto
        {
            ProveedorId = 2,
            ordenes = new() { new() { ProductoId = 20, Cantidad = 3, Precio = 50m } }
        };

        // Actuar
        await service.ActualizarBorradorAsync(5, dto, usuario, esAdministrador: false);

        // Verificar: cambia proveedor, reemplaza líneas y recalcula el total.
        var orden = await context.OrdenesCompra
            .Include(o => o.Detalles)
            .FirstAsync(o => o.Id == 5);
        Assert.Equal(2, orden.ProveedorId);
        Assert.Equal(150m, orden.Total);
        var linea = Assert.Single(orden.Detalles);
        Assert.Equal(20, linea.ProductoId);
        Assert.Equal(3, linea.Cantidad);
    }

    [Fact]
    public async Task ActualizarBorradorAsync_OrdenNoBorrador_LanzaInvalidOperation()
    {
        // Preparar: una orden ya enviada a aprobación.
        using var context = CreateContext();
        var usuario = Guid.NewGuid();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Carlos" });
        context.Productos.Add(new Producto { Id = 10, Nombre = "NVIDIA", ProveedorId = 1 });
        var orden = new OrdenCompra
        {
            Id = 5,
            ProveedorId = 1,
            CreadoPorUsuarioId = usuario,
            Detalles = new() { new DetalleOrden { ProductoId = 10, Cantidad = 1, Precio = 10m } }
        };
        context.OrdenesCompra.Add(orden);
        await context.SaveChangesAsync();
        orden.EnviarAAprobacion();
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null), new TasaFalsa(59m), context, new NotificacionService(context));

        var dto = new CrearOrdenCompraDto
        {
            ProveedorId = 1,
            ordenes = new() { new() { ProductoId = 10, Cantidad = 2, Precio = 10m } }
        };

        // Actuar y verificar: solo se edita en Borrador.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ActualizarBorradorAsync(5, dto, usuario, esAdministrador: false));

        Assert.Contains("Borrador", ex.Message);
    }

    [Fact]
    public async Task ListarAsync_Estandar_SoloDevuelveSusPropiasOrdenes()
    {
        // Preparar: dos órdenes de usuarios distintos.
        using var context = CreateContext();
        var usuario = Guid.NewGuid();
        var otroUsuario = Guid.NewGuid();
        context.OrdenesCompra.AddRange(
            new OrdenCompra { Id = 1, CreadoPorUsuarioId = usuario, Total = 10m },
            new OrdenCompra { Id = 2, CreadoPorUsuarioId = otroUsuario, Total = 20m });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        // Actuar: listar como Estándar.
        var ordenes = await service.ListarAsync(usuario, esAdministrador: false);

        // Verificar: solo aparece la orden propia.
        Assert.Single(ordenes);
        Assert.Equal(1, ordenes[0].Id);
        Assert.Equal("Borrador", ordenes[0].Estado);
    }

    [Fact]
    public async Task ListarAsync_Administrador_DevuelveTodasLasOrdenes()
    {
        // Preparar: dos órdenes de usuarios distintos.
        using var context = CreateContext();
        context.OrdenesCompra.AddRange(
            new OrdenCompra { Id = 1, CreadoPorUsuarioId = Guid.NewGuid(), Total = 10m },
            new OrdenCompra { Id = 2, CreadoPorUsuarioId = Guid.NewGuid(), Total = 20m });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        // Actuar: listar como Administrador.
        var ordenes = await service.ListarAsync(Guid.NewGuid(), esAdministrador: true);

        // Verificar: aparecen las dos, la más reciente primero.
        Assert.Equal(2, ordenes.Count);
        Assert.Equal(2, ordenes[0].Id);
    }
}
