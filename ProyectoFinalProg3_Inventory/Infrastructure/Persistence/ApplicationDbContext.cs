using Application.Interfaces;
using Business.Inventory.Entities;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TokenRevocado> TokensRevocados { get; set; } //Nueva entidad para tokens revocados
        //Entidades del Core (Control de Acceso y Correos)
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<CorreoEnCola> CorreosEnCola { get; set; }

        //Entidades del Modulo de Negocio (Inventario)
        public DbSet<OrdenCompra> OrdenesCompra { get; set; }
        public DbSet<DetalleOrden> DetallesOrden { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configuraciones del Core
            //Esto asegura que la propiedad Email sea única y requerida, y que la propiedad PasswordHash sea requerida.
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Rol).HasMaxLength(20);
            });

            modelBuilder.Entity<CorreoEnCola>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Destinatario).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Asunto).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Estado).IsRequired().HasMaxLength(20);
            });

            //Configuraciones del Modulo de Negocio (Inventario)
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Precio).HasPrecision(18, 2);

                //Relacion Proveedor -> Producto (Uno a Muchos)
                entity.HasOne(p => p.Proveedor)
                      .WithMany(pr => pr.Productos)
                      .HasForeignKey(p => p.ProveedorId)
                      .OnDelete(DeleteBehavior.Restrict);

                //DeleteBehavior.Restrict evita que se elimine un proveedor si tiene productos asociados.
            });

            modelBuilder.Entity<OrdenCompra>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Total).HasPrecision(18, 2);

                //Relacion Proveedor -> OrdenCompra (Uno a Muchos)
                entity.HasOne(o => o.Proveedor)
                      .WithMany(p => p.OrdenesCompra)
                      .HasForeignKey(o => o.ProveedorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DetalleOrden>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Precio).HasPrecision(18, 2);
                entity.Ignore(d => d.Subtotal); //Subtotal es una propiedad calculada, no se almacena en la base de datos.

                //Relacion OrdenCompra -> DetalleOrden (Uno a Muchos)
                entity.HasOne(d => d.OrdenCompra)
                      .WithMany(o => o.Detalles)
                      .HasForeignKey(d => d.OrdenCompraId);

                //Relacion Producto -> DetalleOrden (Uno a Muchos)
                entity.HasOne(d => d.Producto)
                      .WithMany(p => p.DetallesOrden)
                      .HasForeignKey(d => d.ProductoId);
            });
        }
    }
}
