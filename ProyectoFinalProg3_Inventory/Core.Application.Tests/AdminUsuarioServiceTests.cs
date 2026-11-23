using Application.Services;
using Core.Application.DTOs.Admin;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class AdminUsuarioServiceTests
{
    private static ApplicationDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(opciones);
    }

    private static AdminUsuarioService CrearServicio(ApplicationDbContext contexto)
        => new(contexto, new AuditoriaService(contexto));

    // RF-AUD-04: el actor es el administrador y el afectado es otro usuario.
    [Fact]
    public async Task RF_AUD_04_CambiarRol_RegistraAuditoria()
    {
        using var contexto = CrearContexto();
        var admin = new Usuario("admin-aud-rol@test.com", "hash");
        var usuario = new Usuario("objetivo-aud-rol@test.com", "hash");
        contexto.Usuarios.AddRange(admin, usuario);
        await contexto.SaveChangesAsync();

        await CrearServicio(contexto).CambiarRolUsuarioAsync(
            usuario.Id,
            new CambiarRolDto { NuevoRol = RolUsuario.Administrador },
            admin.Id);

        var registro = Assert.Single(contexto.Auditoria);
        Assert.Equal(admin.Id, registro.UsuarioId);
        Assert.Equal(admin.Email, registro.UsuarioEmail);
        Assert.Equal("CambiarRol", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuario.Id.ToString(), registro.EntidadId);
        Assert.Contains("Estandar", registro.ValorAnterior);
        Assert.Contains("Administrador", registro.ValorNuevo);
    }

    [Fact]
    public async Task RF_AUD_04_Desactivar_RegistraAuditoria()
    {
        using var contexto = CrearContexto();
        var admin = new Usuario("admin-aud-estado@test.com", "hash");
        var usuario = new Usuario("objetivo-aud-estado@test.com", "hash");
        usuario.Reactivar(); // El usuario nace inactivo; lo activamos para auditar el cambio.
        contexto.Usuarios.AddRange(admin, usuario);
        await contexto.SaveChangesAsync();

        await CrearServicio(contexto).CambiarEstadoUsuarioAsync(
            usuario.Id,
            new CambiarEstadoDto { Activo = false },
            admin.Id);

        var registro = Assert.Single(contexto.Auditoria);
        Assert.Equal(admin.Id, registro.UsuarioId);
        Assert.Equal("Desactivar", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuario.Id.ToString(), registro.EntidadId);
        Assert.Contains("true", registro.ValorAnterior);
        Assert.Contains("false", registro.ValorNuevo);
    }

    [Fact]
    public async Task RF_AUD_04_Reactivar_RegistraAuditoria()
    {
        using var contexto = CrearContexto();
        var admin = new Usuario("admin-aud-reactivar@test.com", "hash");
        var usuario = new Usuario("objetivo-aud-reactivar@test.com", "hash");
        usuario.Desactivar(admin.Id);
        contexto.Usuarios.AddRange(admin, usuario);
        await contexto.SaveChangesAsync();

        await CrearServicio(contexto).CambiarEstadoUsuarioAsync(
            usuario.Id,
            new CambiarEstadoDto { Activo = true },
            admin.Id);

        var registro = Assert.Single(contexto.Auditoria);
        Assert.Equal(admin.Id, registro.UsuarioId);
        Assert.Equal("Reactivar", registro.Accion);
        Assert.Equal(usuario.Id.ToString(), registro.EntidadId);
        Assert.Contains("false", registro.ValorAnterior);
        Assert.Contains("true", registro.ValorNuevo);
    }
}
