using Application.DTOs.Divisa;
using Application.DTOs.Ordenes;
using Application.Interfaces;
using Business.Inventory.Entities;
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

        public OrdenCompraService(IOrdenCompraRepository repository,
            ITasaCambioService service,
            IApplicationDbContext context)
        {
            _repository = repository;
            _service = service;
            _context = context;
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

        public async Task<int> CrearBorradorAsync(CrearOrdenCompraDto dto, Guid usuarioId)
        {
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

        public async Task EnviarAprobacionAsync(int id)
        {
            var orden = await _repository.GetByIdAsync(id);
            if(orden == null)
            {
                throw new InvalidOperationException("Orden de compra no encontrada");
            }
            orden.EnviarAAprobacion();
            await _repository.SaveChangesAsync();
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
