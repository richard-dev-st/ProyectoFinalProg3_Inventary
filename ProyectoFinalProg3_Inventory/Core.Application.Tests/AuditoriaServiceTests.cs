using Application.DTOs.Auditoria;
using Application.Interfaces;
using Application.Services;
using Core.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class AuditoriaServiceTests
{
    private static ApplicationDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(opciones);
    }

    private static RegistroAuditoria Registro(
        Guid usuarioId, string email, string entidad, DateTime fechaHoraUtc, string accion = "Crear")
        => RegistroAuditoria.Crear(
            usuarioId, email, accion, entidad, Guid.NewGuid().ToString(), fechaHoraUtc);

    private static FiltroAuditoriaDto Filtro(
        Guid? usuarioId = null, string? entidad = null,
        DateTime? desde = null, DateTime? hasta = null,
        int pagina = 1, int tamanioPagina = 20)
        => new(usuarioId, entidad, desde, hasta, pagina, tamanioPagina);

    // RD-06: el rol se comprueba en el servicio, aunque se invoque directamente.
    [Fact]
    public async Task RF_AUD_07_Consultar_SinSerAdministrador_SeRechaza()
    {
        using var contexto = CrearContexto();
        contexto.Auditoria.Add(Registro(Guid.NewGuid(), "actor@test.com", "Documento", DateTime.UtcNow));
        await contexto.SaveChangesAsync();
        var servicio = new AuditoriaService(contexto);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => servicio.ConsultarAsync(false, Filtro()));
    }

    // RF-AUD-07: los filtros por usuario, entidad y fechas se pueden combinar.
    [Fact]
    public async Task RF_AUD_07_Filtros_UsuarioEntidadYFechas_SeCombinan()
    {
        using var contexto = CrearContexto();
        var usuario1 = Guid.NewGuid();
        var usuario2 = Guid.NewGuid();
        var fecha1 = new DateTime(2026, 1, 10, 10, 0, 0);
        var fecha2 = new DateTime(2026, 1, 20, 10, 0, 0);
        var fecha3 = new DateTime(2026, 2, 10, 10, 0, 0);
        var fecha4 = new DateTime(2026, 3, 10, 10, 0, 0);

        contexto.Auditoria.AddRange(
            Registro(usuario1, "u1@test.com", "Documento", fecha1),
            Registro(usuario1, "u1@test.com", "SolicitudPermiso", fecha2),
            Registro(usuario2, "u2@test.com", "Documento", fecha3),
            Registro(usuario2, "u2@test.com", "Usuario", fecha4));
        await contexto.SaveChangesAsync();
        var servicio = new AuditoriaService(contexto);

        var porUsuario = await servicio.ConsultarAsync(true, Filtro(usuarioId: usuario1));
        Assert.Equal(2, porUsuario.Total);
        Assert.All(porUsuario.Registros, r => Assert.Equal(usuario1, r.UsuarioId));

        var porEntidad = await servicio.ConsultarAsync(true, Filtro(entidad: "Documento"));
        Assert.Equal(2, porEntidad.Total);
        Assert.All(porEntidad.Registros, r => Assert.Equal("Documento", r.Entidad));

        var porFechas = await servicio.ConsultarAsync(
            true, Filtro(desde: new DateTime(2026, 1, 1), hasta: new DateTime(2026, 2, 28)));
        Assert.Equal(3, porFechas.Total);

        var combinado = await servicio.ConsultarAsync(
            true, Filtro(usuario1, "Documento", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)));

        var registro = Assert.Single(combinado.Registros);
        Assert.Equal(1, combinado.Total);
        Assert.Equal(usuario1, registro.UsuarioId);
        Assert.Equal("u1@test.com", registro.UsuarioEmail);
        Assert.Equal("Documento", registro.Entidad);
        Assert.Equal("Crear", registro.Accion);
        Assert.Equal(fecha1, registro.FechaHoraUtc);
    }

    // RF-AUD-07: total filtrado completo, página parcial y orden más reciente primero.
    [Fact]
    public async Task RF_AUD_07_Paginacion_TotalYOrdenDescendente()
    {
        using var contexto = CrearContexto();
        for (var dia = 1; dia <= 5; dia++)
        {
            contexto.Auditoria.Add(Registro(
                Guid.NewGuid(), "actor@test.com", "Documento", new DateTime(2026, 1, dia, 12, 0, 0)));
        }
        await contexto.SaveChangesAsync();
        var servicio = new AuditoriaService(contexto);

        var resultado = await servicio.ConsultarAsync(true, Filtro(pagina: 2, tamanioPagina: 2));

        Assert.Equal(5, resultado.Total);
        Assert.Equal(2, resultado.Pagina);
        Assert.Equal(2, resultado.TamanioPagina);
        var fechas = resultado.Registros.Select(r => r.FechaHoraUtc).ToList();
        Assert.Equal(2, fechas.Count);
        Assert.Equal(new DateTime(2026, 1, 3, 12, 0, 0), fechas[0]);
        Assert.Equal(new DateTime(2026, 1, 2, 12, 0, 0), fechas[1]);
    }

    // RF-AUD-07: una fecha sin hora incluye el día completo; con hora respeta el instante.
    [Fact]
    public async Task RF_AUD_07_HastaSinHora_IncluyeDiaCompleto()
    {
        using var contexto = CrearContexto();
        contexto.Auditoria.AddRange(
            Registro(Guid.NewGuid(), "a@test.com", "Documento", new DateTime(2026, 1, 10, 0, 0, 0)),
            Registro(Guid.NewGuid(), "b@test.com", "Documento", new DateTime(2026, 1, 10, 23, 59, 59)),
            Registro(Guid.NewGuid(), "c@test.com", "Documento", new DateTime(2026, 1, 11, 0, 0, 0)));
        await contexto.SaveChangesAsync();
        var servicio = new AuditoriaService(contexto);

        var diaCompleto = await servicio.ConsultarAsync(
            true, Filtro(hasta: new DateTime(2026, 1, 10)));
        Assert.Equal(2, diaCompleto.Total);

        var conHora = await servicio.ConsultarAsync(
            true, Filtro(hasta: new DateTime(2026, 1, 10, 12, 0, 0)));
        Assert.Equal(1, conHora.Total);
    }

    // RD-07: el extremo máximo de DateTime no debe desbordar al hacer AddDays(1).
    [Fact]
    public async Task RF_AUD_07_HastaFechaMaxima_NoDesborda()
    {
        using var contexto = CrearContexto();
        contexto.Auditoria.Add(Registro(
            Guid.NewGuid(), "actor@test.com", "Documento", DateTime.MaxValue));
        await contexto.SaveChangesAsync();
        var servicio = new AuditoriaService(contexto);

        var resultado = await servicio.ConsultarAsync(
            true, Filtro(hasta: new DateTime(9999, 12, 31)));

        Assert.Equal(1, resultado.Total);
    }

    // RF-AUD-06: el contrato solo expone preparar y consultar; nada edita ni borra registros.
    [Fact]
    public async Task RF_AUD_06_OperacionesExpuestas_NoModificanNiBorranRegistros()
    {
        using var contexto = CrearContexto();
        var original = Registro(Guid.NewGuid(), "actor@test.com", "Documento", new DateTime(2026, 1, 10, 10, 0, 0));
        contexto.Auditoria.Add(original);
        await contexto.SaveChangesAsync();
        var servicio = new AuditoriaService(contexto);

        var nombres = typeof(IAuditoriaService).GetMethods().Select(m => m.Name).ToList();
        Assert.Equal(new[] { "ConsultarAsync", "Preparar" }, nombres.OrderBy(n => n));
        Assert.DoesNotContain(nombres, n => n.Contains("Eliminar") || n.Contains("Borrar")
            || n.Contains("Editar") || n.Contains("Modificar") || n.Contains("Actualizar"));

        // Preparar solo construye la entidad; ConsultarAsync usa AsNoTracking.
        servicio.Preparar(Guid.NewGuid(), "otro@test.com", "Crear", "Documento", Guid.NewGuid().ToString());
        var consulta = await servicio.ConsultarAsync(true, Filtro());

        Assert.Single(contexto.Auditoria);
        var persistido = Assert.Single(consulta.Registros);
        Assert.Equal(original.Id, persistido.Id);
        Assert.Equal(original.UsuarioId, persistido.UsuarioId);
        Assert.Equal(original.UsuarioEmail, persistido.UsuarioEmail);
        Assert.Equal(original.Accion, persistido.Accion);
        Assert.Equal(original.Entidad, persistido.Entidad);
        Assert.Equal(original.FechaHoraUtc, persistido.FechaHoraUtc);
        Assert.DoesNotContain(contexto.ChangeTracker.Entries<RegistroAuditoria>(),
            e => e.State != EntityState.Unchanged);
    }

    // RD-07: los filtros inválidos se rechazan con un mensaje controlado.
    [Fact]
    public async Task RD_07_FiltrosInvalidos_SeRechazan()
    {
        using var contexto = CrearContexto();
        var servicio = new AuditoriaService(contexto);

        var paginaCero = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ConsultarAsync(true, Filtro(pagina: 0)));
        Assert.Contains("página", paginaCero.Message);

        var tamanioExcesivo = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ConsultarAsync(true, Filtro(tamanioPagina: 101)));
        Assert.Contains("tamaño de página", tamanioExcesivo.Message);

        var rangoInvertido = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ConsultarAsync(true, Filtro(
                desde: new DateTime(2026, 2, 1), hasta: new DateTime(2026, 1, 1))));
        Assert.Contains("rango de fechas", rangoInvertido.Message);
    }
}
