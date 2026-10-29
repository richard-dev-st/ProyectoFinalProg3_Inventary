using Application.Services;
using Business.Inventory.Entities;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bussiness.inventory.Tests
{
    /// <summary>
    /// Prueba de integración (Práctica 3, punto 1.4).
    /// Usa los componentes reales de Gestión de Permisos (SolicitudPermisoService) y del
    /// módulo de negocio (OrdenCompraService + OrdenCompraRepository), sin dobles y sin
    /// levantar la aplicación completa (RD-12).
    /// </summary>
    public class OrdenCompraIntegracionTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Recibir_SinPermisoAprobado_Rechaza_YConPermisoAprobado_Permite()
        {
            // Arrange
            using var context = CreateContext();

            var usuarioEstandarId = Guid.NewGuid();
            var usuarioAdminId = Guid.NewGuid();

            // Usuarios reales del dominio (los setters son privados: se ajustan por reflexión para el test)
            var usuarioEstandar = new Usuario("estandar@test.com", "hash123");
            var usuarioAdmin = new Usuario("admin@test.com", "hash123");

            typeof(Usuario).GetProperty("Id")!.SetValue(usuarioEstandar, usuarioEstandarId);
            typeof(Usuario).GetProperty("Id")!.SetValue(usuarioAdmin, usuarioAdminId);
            typeof(Usuario).GetProperty("Rol")!.SetValue(usuarioAdmin, RolUsuario.Administrador);
            typeof(Usuario).GetProperty("Activo")!.SetValue(usuarioEstandar, true);
            typeof(Usuario).GetProperty("Activo")!.SetValue(usuarioAdmin, true);

            context.Usuarios.Add(usuarioEstandar);
            context.Usuarios.Add(usuarioAdmin);
            await context.SaveChangesAsync();

            // Orden de compra real, llevada al estado Aprobada
            var orden = new OrdenCompra
            {
                ProveedorId = 1,
                CreadoPorUsuarioId = usuarioEstandarId,
                Detalles = new List<DetalleOrden>
                {
                    new DetalleOrden { ProductoId = 1, Cantidad = 10, Precio = 10 }
                }
            };

            context.OrdenesCompra.Add(orden);
            await context.SaveChangesAsync();

            orden.EnviarAAprobacion();
            orden.Aprobar();
            await context.SaveChangesAsync();

            // Componentes reales de ambos módulos
            var ordenRepository = new OrdenCompraRepository(context);
            var ordenService = new OrdenCompraService(ordenRepository, context);
            var solicitudService = new SolicitudPermisoService(context);

            // Act + Assert (cara 1): sin el permiso aprobado la operación se rechaza
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => ordenService.RecibirAsync(orden.Id, usuarioEstandarId));

            // Assert (cara 1): el estado de la orden no cambió
            Assert.Equal(EstadoOrden.Aprobada, orden.Estado);

            // Act: el usuario Estándar solicita el permiso a través de Gestión de Permisos
            var solicitud = await solicitudService.CrearAsync(
                usuarioEstandarId,
                "recibir-orden",
                "Necesito recibir la orden de compra");

            // Assert: la solicitud pendiente todavía no concede el permiso
            Assert.False(context.PermisosUsuario.Any(p =>
                p.UsuarioId == usuarioEstandarId && p.Permiso == "recibir-orden"));

            // Act: un Administrador aprueba la solicitud (RF-GP-05 registra el permiso)
            await solicitudService.AprobarAsync(solicitud.Id, usuarioAdminId);

            // Assert: la aprobación concede el permiso
            Assert.True(context.PermisosUsuario.Any(p =>
                p.UsuarioId == usuarioEstandarId && p.Permiso == "recibir-orden"));

            // Act + Assert (cara 2): con el permiso aprobado la operación se permite
            await ordenService.RecibirAsync(orden.Id, usuarioEstandarId);

            Assert.Equal(EstadoOrden.Recibida, orden.Estado);
        }
    }
}
