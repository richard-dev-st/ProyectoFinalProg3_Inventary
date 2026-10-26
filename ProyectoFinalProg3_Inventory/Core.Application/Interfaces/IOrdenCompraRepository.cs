using Business.Inventory.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IOrdenCompraRepository
    {
        Task<OrdenCompra> GetByIdAsync(int id);
        Task AddAsync(OrdenCompra orden);
        Task SaveChangesAsync();
    }
}
