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

        public OrdenCompraService(IOrdenCompraRepository repository)
        {
            _repository = repository;
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

        public async Task RecibirAsync(int id)
        {
            var orden = await _repository.GetByIdAsync(id);
            if (orden == null)
            {
                throw new InvalidOperationException("Orden de compra no encontrada");
            }
            orden.Recibir();
            await _repository.SaveChangesAsync();
        }
    }
}
