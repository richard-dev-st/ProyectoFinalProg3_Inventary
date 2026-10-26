using Application.Interfaces;
using Business.Inventory.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class OrdenCompraRepository : IOrdenCompraRepository
    {
        private readonly ApplicationDbContext _context;
        public OrdenCompraRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OrdenCompra orden)
        {
            await _context.Set<OrdenCompra>().AddAsync(orden);
        }

        public async Task<OrdenCompra> GetByIdAsync(int id)
        {
            return await _context.Set<OrdenCompra>().Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
