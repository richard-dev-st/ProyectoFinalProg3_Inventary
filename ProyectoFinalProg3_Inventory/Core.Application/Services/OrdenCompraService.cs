using Application.DTOs.Divisa;
using Application.DTOs.Ordenes;
using Application.Interfaces;
using Business.Inventory.Entities;
using Core.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly IOrdenCompraRepository _repository;
        private readonly ITasaCambioService _service;
        private readonly IApplicationDbContext _context;
        private readonly INotificacionService _notificaciones;

        public OrdenCompraService(IOrdenCompraRepository repository,
            ITasaCambioService service,
            IApplicationDbContext context,
            INotificacionService notificaciones)
        {
            _repository = repository;
            _service = service;
            _context = context;
            _notificaciones = notificaciones;
        }

        // RF-NEG-02: el listado es de solo lectura; el Estándar ve únicamente sus órdenes.
        public async Task<List<OrdenCompraResumenDto>> ListarAsync(Guid usuarioId, bool esAdministrador)
        {
            var consulta = _context.OrdenesCompra.AsNoTracking().AsQueryable();
            if (!esAdministrador)
            {
                consulta = consulta.Where(o => o.CreadoPorUsuarioId == usuarioId);
            }

            return await consulta
                .OrderByDescending(o => o.Id)
                .Select(o => new OrdenCompraResumenDto(
                    o.Id,
                    o.ProveedorId,
                    o.Estado.ToString(),
                    o.Total,
                    o.FechaCreacion,
                    o.CreadoPorUsuarioId,
                    o.Detalles.Count))
                .ToListAsync();
        }

        public async Task AprobarAsync(int id)
        {
            var orden = await _repository.GetByIdAsync(id);
            if(orden == null)
            {
                throw new InvalidOperationException("Orden de compra no encontrada");
            }
            orden.Aprobar();
            await _repository.SaveChangesAsync();
        }

        public async Task CancelarAsync(int id)
        {
            var orden = await _repository.GetByIdAsync(id);
            if(orden == null)
            {
                throw new InvalidOperationException("Orden de compra no encontrada");
            }
            orden.Cancelar();
            await _repository.SaveChangesAsync();
        }

        // RF-NEG-02: detalle de una orden con sus líneas y el proveedor.
        public async Task<OrdenCompraDetalleDto?> ObtenerDetalleAsync(
            int id, Guid usuarioId, bool esAdministrador)
        {
            var orden = await _context.OrdenesCompra.AsNoTracking()
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden is null)
            {
                return null;
            }

            // Igual que en el listado: el Estándar solo ve sus propias órdenes.
            if (!esAdministrador && orden.CreadoPorUsuarioId != usuarioId)
            {
                throw new UnauthorizedAccessException(
                    "No puedes ver una orden de otro usuario.");
            }

            return new OrdenCompraDetalleDto(
                orden.Id,
                orden.ProveedorId,
                orden.Proveedor?.Nombre ?? string.Empty,
                orden.Estado.ToString(),
                orden.Total,
                orden.FechaCreacion,
                orden.CreadoPorUsuarioId,
                orden.Detalles.Select(d => new DetalleOrdenLineaDto(
                    d.ProductoId,
                    d.Producto?.Nombre ?? string.Empty,
                    d.Producto?.Codigo ?? string.Empty,
                    d.Cantidad,
                    d.Precio,
                    d.Subtotal)).ToList());
        }

        // RF-NEG-02: solo se edita mientras la orden siga en Borrador.
        public async Task ActualizarBorradorAsync(
            int id, CrearOrdenCompraDto dto, Guid usuarioId, bool esAdministrador)
        {
            var orden = await _context.OrdenesCompra
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden is null)
            {
                throw new InvalidOperationException("Orden de compra no encontrada.");
            }

            if (orden.Estado != EstadoOrden.Borrador)
            {
                throw new InvalidOperationException(
                    "Solo se puede editar una orden en estado Borrador.");
            }

            if (!esAdministrador && orden.CreadoPorUsuarioId != usuarioId)
            {
                throw new UnauthorizedAccessException(
                    "No puedes editar una orden de otro usuario.");
            }

            await ValidarProveedorYProductosAsync(dto);

            orden.ProveedorId = dto.ProveedorId;

            _context.DetallesOrden.RemoveRange(orden.Detalles);
            orden.Detalles = dto.ordenes.Select(d => new DetalleOrden
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                Precio = d.Precio
            }).ToList();

            orden.Total = orden.Detalles.Sum(d => d.Subtotal);

            await _context.SaveChangesAsync();
        }

        public async Task<int> CrearBorradorAsync(CrearOrdenCompraDto dto, Guid usuarioId)
        {
            // RD-07: entrada inválida se rechaza con un mensaje claro antes de tocar la BD.
            await ValidarProveedorYProductosAsync(dto);

            var orden = new OrdenCompra
            {
                ProveedorId = dto.ProveedorId,
                CreadoPorUsuarioId = usuarioId,
                FechaCreacion = DateTime.UtcNow,
                Detalles = dto.ordenes.Select(d => new DetalleOrden{
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    Precio = d.Precio
                }).ToList()
            };

            orden.Total = orden.Detalles.Sum(d => d.Subtotal);

            await _repository.AddAsync(orden);
            await _repository.SaveChangesAsync();
            return orden.Id;
        }

        // RD-07 / RF-NEG-02: valida proveedor, líneas y que cada producto sea del proveedor.
        // Se comparte entre crear y editar para que ambas entradas apliquen la misma regla.
        private async Task ValidarProveedorYProductosAsync(CrearOrdenCompraDto dto)
        {
            if (!await _context.Proveedores.AnyAsync(p => p.Id == dto.ProveedorId))
            {
                throw new InvalidOperationException(
                    $"El proveedor {dto.ProveedorId} no existe.");
            }

            if (dto.ordenes.Count == 0)
            {
                throw new InvalidOperationException(
                    "La orden debe incluir al menos un producto.");
            }

            foreach (var detalle in dto.ordenes)
            {
                var producto = await _context.Productos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);

                if (producto is null)
                {
                    throw new InvalidOperationException(
                        $"El producto {detalle.ProductoId} no existe.");
                }

                if (producto.ProveedorId != dto.ProveedorId)
                {
                    throw new InvalidOperationException(
                        $"El producto {detalle.ProductoId} no pertenece al proveedor {dto.ProveedorId}.");
                }
            }
        }

        public async Task EnviarAprobacionAsync(int id)
        {
            var orden = await _repository.GetByIdAsync(id);
            if(orden == null)
            {
                throw new InvalidOperationException("Orden de compra no encontrada");
            }
            orden.EnviarAAprobacion();

            // RF-NOT-03: evento del módulo de negocio; avisa a los Administradores activos.
            // Hallazgo 2: la notificación se prepara y se guarda junto con la transición.
            await _notificaciones.PrepararAdministradoresAsync(
                $"La orden de compra #{orden.Id} por {orden.Total:0.00} USD fue enviada a aprobación.",
                NotificacionService.TipoOrdenEnviadaAprobacion,
                orden.Id.ToString());

            await _context.SaveChangesAsync();
        }

        public async Task<TotalEnDopDto> ObtenerTotalEnDopAsync(int ordenId, CancellationToken ct = default)
        {
            var orden = await _repository.GetByIdAsync(ordenId);
            if(orden == null)
            {
                //Lanzamos 404
                throw new InvalidOperationException("Orden de compra no encontrada");
            }

            var tasaCambio = await _service.ObtenerTasaAsync("USD", "DOP", ct);

            //Se redondea a 2 decimales porque es un monto de dinero.
            var totalDop = decimal.Round(
                orden.Total * tasaCambio.Tasa,
                2,
                MidpointRounding.AwayFromZero);

            //Devolvemos el DTO sin persistirlo ni modificar la orden, solo para mostrar el total en DOP
            return new TotalEnDopDto(
                OrdenId: orden.Id,
                TotalUsd: orden.Total,
                Tasa: tasaCambio.Tasa,
                TotalDop: totalDop,
                ActualizadoEn: tasaCambio.ActualizadoEn
            );
        }

        public async Task RecibirAsync(int id, Guid usuarioId)
        {
            var orden = await _repository.GetByIdAsync(id);
            if (orden == null)
            {
                throw new InvalidOperationException("Orden de compra no encontrada");
            }

            bool tienePermiso = _context.PermisosUsuario.Any(p => p.UsuarioId == usuarioId && p.Permiso == "recibir-orden");
            
            if (!tienePermiso)
            {
                throw new UnauthorizedAccessException("No posees el permiso 'recibir-orden'. Debe solicitarlo a través de gestión de permisos.");
            }

            orden.Recibir();
            await _repository.SaveChangesAsync();
        }
    }
}
