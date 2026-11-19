using Application.Interfaces;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Application.Tests;

public class ProcesadorCorreosTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ProcesadorCorreos CrearProcesador(ApplicationDbContext context, IEmailService email)
        => new(context, email, NullLogger<ProcesadorCorreos>.Instance);

    private static async Task<CorreoEnCola> CrearCorreoPendiente(ApplicationDbContext context)
    {
        var correo = new CorreoEnCola("destino@test.com", "Asunto de prueba", "<p>Cuerpo</p>");
        context.CorreosEnCola.Add(correo);
        await context.SaveChangesAsync();
        return correo;
    }

    // RF-NOT-09: el procesador independiente toma el pendiente y lo envía.
    [Fact]
    public async Task RF_NOT_09_ProcesarPendiente_LoMarcaComoEnviado()
    {
        using var context = CreateContext();
        var correo = await CrearCorreoPendiente(context);
        var email = new EmailFalso();

        var procesados = await CrearProcesador(context, email).ProcesarPendientesAsync();

        Assert.Equal(1, procesados);
        Assert.Equal(1, email.Llamadas);
        Assert.Equal(CorreoEnCola.EstadoEnviado, correo.Estado);
        Assert.Equal(1, correo.Intentos);
        Assert.NotNull(correo.FechaEnvio);
        Assert.NotNull(correo.FechaUltimoIntento);
    }

    // RF-NOT-10: reintentos limitados; al llegar al máximo queda Fallido con su último error.
    [Fact]
    public async Task RF_NOT_10_FalloPersistente_AgotaIntentosYQuedaFallido()
    {
        using var context = CreateContext();
        var correo = await CrearCorreoPendiente(context);
        var email = new EmailFalso { Resultado = false };

        var procesador = CrearProcesador(context, email);

        await procesador.ProcesarPendientesAsync();
        await procesador.ProcesarPendientesAsync();
        await procesador.ProcesarPendientesAsync();

        Assert.Equal(ProcesadorCorreos.MaximoIntentos, correo.Intentos);
        Assert.Equal(CorreoEnCola.EstadoFallido, correo.Estado);
        Assert.NotNull(correo.UltimoError);
        Assert.NotNull(correo.FechaUltimoIntento);

        // Un ciclo más no vuelve a intentarlo ni cambia el estado.
        await procesador.ProcesarPendientesAsync();

        Assert.Equal(ProcesadorCorreos.MaximoIntentos, email.Llamadas);
        Assert.Equal(ProcesadorCorreos.MaximoIntentos, correo.Intentos);
        Assert.Equal(CorreoEnCola.EstadoFallido, correo.Estado);
    }

    // RF-NOT-10: una excepción del SMTP también deja rastro en UltimoError.
    [Fact]
    public async Task RF_NOT_10_ExcepcionSmtp_RegistraUltimoError()
    {
        using var context = CreateContext();
        var correo = await CrearCorreoPendiente(context);
        var email = new EmailFalso { Error = new InvalidOperationException("SMTP no disponible") };

        await CrearProcesador(context, email).ProcesarPendientesAsync();

        Assert.Equal(CorreoEnCola.EstadoPendiente, correo.Estado);
        Assert.Equal(1, correo.Intentos);
        Assert.Contains("SMTP no disponible", correo.UltimoError);
    }

    // RF-NOT-10: si el servicio devuelve false, se guarda el mensaje del error del SMTP.
    [Fact]
    public async Task RF_NOT_10_ServicioDevuelveFalse_GuardaMensajeDelError()
    {
        using var context = CreateContext();
        var correo = await CrearCorreoPendiente(context);
        var email = new EmailFalso
        {
            Resultado = false,
            UltimoError = "No se pudo conectar con el servidor SMTP."
        };

        await CrearProcesador(context, email).ProcesarPendientesAsync();

        Assert.Equal(1, correo.Intentos);
        Assert.Contains("servidor SMTP", correo.UltimoError);
    }

    // RF-NOT-12: ejecutar el procesador dos veces no reenvía ni duplica.
    [Fact]
    public async Task RF_NOT_12_ProcesarDosVeces_NoReenvia()
    {
        using var context = CreateContext();
        var correo = await CrearCorreoPendiente(context);
        var email = new EmailFalso();
        var procesador = CrearProcesador(context, email);

        await procesador.ProcesarPendientesAsync();
        var fechaEnvio = correo.FechaEnvio;

        await procesador.ProcesarPendientesAsync();

        Assert.Equal(1, email.Llamadas);
        Assert.Equal(1, correo.Intentos);
        Assert.Equal(fechaEnvio, correo.FechaEnvio);
        Assert.Single(context.CorreosEnCola);
    }

    private sealed class EmailFalso : IEmailService
    {
        public bool Resultado { get; init; } = true;
        public Exception? Error { get; init; }
        public string? UltimoError { get; set; }
        public int Llamadas { get; private set; }

        public Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            Llamadas++;

            if (Error != null)
            {
                throw Error;
            }

            return Task.FromResult(Resultado);
        }
    }
}
