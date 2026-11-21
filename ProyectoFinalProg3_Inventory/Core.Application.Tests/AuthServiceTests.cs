using Application.DTOs.Auth;
using Application.Services;
using Core.Application.Configurations;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class AuthServiceTests
{
    private sealed class JwtFalso : IJwtService
    {
        public string GenerarToken(Usuario usuario) => "token-falso";
    }

    private static ApplicationDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(opciones);
    }

    private static AuthService CrearServicio(
        ApplicationDbContext contexto,
        string baseUrlFrontend = "http://localhost:5299")
    {
        return new AuthService(
            contexto,
            new PasswordService(),
            new JwtFalso(),
            new JwtSettings { ExpirationInHours = 8 },
            new FrontendSettings { BaseUrl = baseUrlFrontend });
    }

    private static RegistroRequestDto Registro(string email, string password)
        => new(email, password, "http://localhost:8080");

    private static async Task<Usuario> CrearUsuarioActivoAsync(
        ApplicationDbContext contexto, string email, string password = "Clave1234")
    {
        var servicio = CrearServicio(contexto);
        await servicio.RegistrarAsync(Registro(email, password));
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Email == email);
        await servicio.ActivarCuentaAsync(usuario.TokenActivacion!);
        return usuario;
    }

    private static bool ClaveValida(Usuario usuario, string clave) =>
        new PasswordService().VerifyPassword(clave, usuario.PasswordHash);

    private static void EstablecerPropiedadPrivada(Usuario usuario, string nombre, object? valor) =>
        typeof(Usuario).GetProperty(nombre)!.GetSetMethod(nonPublic: true)!
            .Invoke(usuario, new object?[] { valor });

    [Fact]
    public async Task RF_CA_01_RegistrarConCorreoDuplicado_RechazaElSegundoRegistro()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);

        var primero = await servicio.RegistrarAsync(Registro("Dup@Test.com", "Clave1234"));
        var segundo = await servicio.RegistrarAsync(Registro("dup@test.com", "Clave1234"));

        Assert.True(primero.Exito);
        Assert.False(segundo.Exito);
        Assert.Equal("El correo electrónico ya está registrado.", segundo.Mensaje);
        Assert.Single(contexto.Usuarios);
    }

    [Fact]
    public async Task RF_CA_02_RegistrarDosUsuariosConLaMismaClave_GuardaHashesDistintos()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);

        await servicio.RegistrarAsync(Registro("uno@test.com", "Clave1234"));
        await servicio.RegistrarAsync(Registro("dos@test.com", "Clave1234"));

        var primero = await contexto.Usuarios.SingleAsync(u => u.Email == "uno@test.com");
        var segundo = await contexto.Usuarios.SingleAsync(u => u.Email == "dos@test.com");

        Assert.NotEqual("Clave1234", primero.PasswordHash);
        Assert.NotEqual("Clave1234", segundo.PasswordHash);
        Assert.NotEqual(primero.PasswordHash, segundo.PasswordHash);
    }

    [Fact]
    public async Task RF_CA_14_RegistrarConClaveDebil_NoCreaUsuario()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);

        var resultado = await servicio.RegistrarAsync(Registro("debil@test.com", "solenumeros"));

        Assert.False(resultado.Exito);
        Assert.Contains("letra y un número", resultado.Mensaje);
        Assert.Empty(contexto.Usuarios);
    }

    [Fact]
    public async Task RF_CA_15_Registrar_CreaUsuarioInactivoYEncolaEnlaceDelFrontend()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);

        var resultado = await servicio.RegistrarAsync(Registro("nuevo@test.com", "Clave1234"));

        Assert.True(resultado.Exito);
        var usuario = Assert.Single(contexto.Usuarios);
        Assert.False(usuario.Activo);
        Assert.False(string.IsNullOrWhiteSpace(usuario.TokenActivacion));
        Assert.NotNull(usuario.TokenActivacionExpiracion);

        var correo = Assert.Single(contexto.CorreosEnCola);
        Assert.Contains($"http://localhost:5299/activar?token={usuario.TokenActivacion}", correo.Cuerpo);
        Assert.DoesNotContain("/api/auth/activar", correo.Cuerpo);
    }

    [Fact]
    public async Task RF_CA_16_ActivarConTokenValido_LaCuentaSeActivaYElTokenSeAgota()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        await servicio.RegistrarAsync(Registro("activar@test.com", "Clave1234"));
        var token = (await contexto.Usuarios.SingleAsync()).TokenActivacion!;

        var primera = await servicio.ActivarCuentaAsync(token);
        var segunda = await servicio.ActivarCuentaAsync(token);

        Assert.True(primera.Exito);
        Assert.False(segunda.Exito);
        var usuario = await contexto.Usuarios.SingleAsync();
        Assert.True(usuario.Activo);
        Assert.Null(usuario.TokenActivacion);
        Assert.Null(usuario.TokenActivacionExpiracion);
    }

    [Fact]
    public async Task RF_CA_16_ActivarConTokenVencido_NoCambiaElEstado()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        await servicio.RegistrarAsync(Registro("vencido@test.com", "Clave1234"));
        var usuario = await contexto.Usuarios.SingleAsync();
        var token = usuario.TokenActivacion!;

        typeof(Usuario).GetProperty(nameof(Usuario.TokenActivacionExpiracion))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(usuario, new object?[] { DateTime.UtcNow.AddMinutes(-1) });
        await contexto.SaveChangesAsync();

        var resultado = await servicio.ActivarCuentaAsync(token);

        Assert.False(resultado.Exito);
        Assert.False(usuario.Activo);
    }

    [Fact]
    public async Task RF_CA_17_ReenviarActivacion_RespuestaIgualYTokenAnteriorQuedaInvalido()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        await servicio.RegistrarAsync(Registro("reenvio@test.com", "Clave1234"));
        var usuario = await contexto.Usuarios.SingleAsync();
        var tokenAnterior = usuario.TokenActivacion!;

        var existente = await servicio.ReenviarActivacionAsync("reenvio@test.com", "http://localhost:8080");
        var inexistente = await servicio.ReenviarActivacionAsync("noexiste@test.com", "http://localhost:8080");

        Assert.True(existente.Exito);
        Assert.Equal(inexistente.Mensaje, existente.Mensaje);
        Assert.NotEqual(tokenAnterior, usuario.TokenActivacion);
        Assert.Equal(2, contexto.CorreosEnCola.Count());
        Assert.Contains($"http://localhost:5299/activar?token={usuario.TokenActivacion}", contexto.CorreosEnCola.Last().Cuerpo);
        Assert.False((await servicio.ActivarCuentaAsync(tokenAnterior)).Exito);
    }

    [Fact]
    public async Task RF_CA_09_RecuperarPassword_NoEncolaCorreoParaUnDesconocido()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        await CrearUsuarioActivoAsync(contexto, "recuperacion@test.com");

        await servicio.SolicitarRecuperacionPasswordAsync(
            new SolicitarRecuperacionDto { Email = "recuperacion@test.com" });
        await servicio.SolicitarRecuperacionPasswordAsync(
            new SolicitarRecuperacionDto { Email = "noexiste@test.com" });

        // RF-CA-09: el controlador responde siempre lo mismo; el servicio solo actúa
        // cuando el correo existe y está activo. El otro correo es el de activación.
        var recuperaciones = contexto.CorreosEnCola.Where(c => c.Asunto.Contains("recuperación")).ToList();
        Assert.Single(recuperaciones);
        Assert.Contains("recuperacion@test.com", recuperaciones[0].Destinatario);
    }

    [Fact]
    public async Task RF_CA_10_RestablecerConCodigoUsado_NoCambiaLaClave()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        var usuario = await CrearUsuarioActivoAsync(contexto, "codigo@test.com");
        await servicio.SolicitarRecuperacionPasswordAsync(
            new SolicitarRecuperacionDto { Email = usuario.Email });
        var codigo = usuario.CodigoRecuperacion!;
        var hashInicial = usuario.PasswordHash;

        await servicio.RestablecerPasswordAsync(new RestablecerPasswordDto
        {
            Email = usuario.Email, Codigo = codigo, NuevaPassword = "Nueva1234"
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.RestablecerPasswordAsync(new RestablecerPasswordDto
            {
                Email = usuario.Email, Codigo = codigo, NuevaPassword = "Otra12345"
            }));

        Assert.NotEqual(hashInicial, usuario.PasswordHash);
        Assert.True(ClaveValida(usuario, "Nueva1234"));
        Assert.Null(usuario.CodigoRecuperacion);
    }

    [Fact]
    public async Task RF_CA_10_RestablecerConCodigoVencido_NoCambiaLaClave()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        var usuario = await CrearUsuarioActivoAsync(contexto, "vencido@test.com");
        await servicio.SolicitarRecuperacionPasswordAsync(
            new SolicitarRecuperacionDto { Email = usuario.Email });
        var hashInicial = usuario.PasswordHash;
        EstablecerPropiedadPrivada(usuario, "CodigoRecuperacionExpiracion", DateTime.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.RestablecerPasswordAsync(new RestablecerPasswordDto
            {
                Email = usuario.Email, Codigo = usuario.CodigoRecuperacion!, NuevaPassword = "Nueva1234"
            }));

        Assert.Equal(hashInicial, usuario.PasswordHash);
    }

    [Fact]
    public async Task RF_CA_11_RestablecerConCodigoValido_GuardaHashYActualizaFechaDeCambio()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        var usuario = await CrearUsuarioActivoAsync(contexto, "hash@test.com");
        var fechaInicial = usuario.FechaUltimoCambioPassword;
        await servicio.SolicitarRecuperacionPasswordAsync(
            new SolicitarRecuperacionDto { Email = usuario.Email });

        await servicio.RestablecerPasswordAsync(new RestablecerPasswordDto
        {
            Email = usuario.Email, Codigo = usuario.CodigoRecuperacion!, NuevaPassword = "Nueva1234"
        });

        Assert.NotEqual("Nueva1234", usuario.PasswordHash);
        Assert.True(ClaveValida(usuario, "Nueva1234"));
        Assert.False(ClaveValida(usuario, "Clave1234"));
        // RF-CA-12: OnTokenValidated compara esta fecha con la emisión del JWT.
        Assert.NotEqual(fechaInicial, usuario.FechaUltimoCambioPassword);
    }

    [Fact]
    public async Task RF_CA_13_ForzarRestablecimiento_RevocaClaveYGeneraCodigo()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        var usuario = await CrearUsuarioActivoAsync(contexto, "forzado@test.com");
        var fechaInicial = usuario.FechaUltimoCambioPassword;

        await servicio.ForzarRestablecimientoPasswordAsync(usuario.Id);

        Assert.False(ClaveValida(usuario, "Clave1234"));
        Assert.NotEqual(fechaInicial, usuario.FechaUltimoCambioPassword);
        Assert.NotNull(usuario.CodigoRecuperacion);
        var restablecimientos = contexto.CorreosEnCola
            .Where(c => c.Asunto.Contains("Restablecimiento")).ToList();
        Assert.Single(restablecimientos);
        Assert.Contains(usuario.CodigoRecuperacion!, restablecimientos[0].Cuerpo);
    }

    [Fact]
    public async Task RF_CA_22_CambiarPassword_VerificaLaClaveActualYActualizaLaFecha()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(contexto);
        var usuario = await CrearUsuarioActivoAsync(contexto, "cambio@test.com");
        var fechaInicial = usuario.FechaUltimoCambioPassword;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CambiarPasswordAsync(usuario.Id, new CambiarPasswordDto
            {
                PasswordActual = "Incorrecta1", NuevaPassword = "Nueva1234"
            }));
        Assert.True(ClaveValida(usuario, "Clave1234"));

        await servicio.CambiarPasswordAsync(usuario.Id, new CambiarPasswordDto
        {
            PasswordActual = "Clave1234", NuevaPassword = "Nueva1234"
        });

        Assert.True(ClaveValida(usuario, "Nueva1234"));
        Assert.False(ClaveValida(usuario, "Clave1234"));
        Assert.NotEqual(fechaInicial, usuario.FechaUltimoCambioPassword);
    }
}
