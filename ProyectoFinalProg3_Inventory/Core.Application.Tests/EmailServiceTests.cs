using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Application.Tests;

public class EmailServiceTests
{
    // Logger mínimo para comprobar que el fallback deja el correo en el log del worker.
    private sealed class LoggerDePrueba<T> : ILogger<T>
    {
        public List<string> Mensajes { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => new ScopeNulo();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Mensajes.Add(formatter(state, exception));

        private sealed class ScopeNulo : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private static EmailService CrearServicio(
        LoggerDePrueba<EmailService> logger,
        string? smtpServer = "smtp.gmail.com",
        string? senderEmail = "tu_correo@gmail.com",
        string? senderPassword = "TU_PASSWORD_DE_APLICACION")
    {
        var valores = new Dictionary<string, string?>
        {
            ["EmailSettings:SmtpServer"] = smtpServer,
            ["EmailSettings:SmtpPort"] = "587",
            ["EmailSettings:SenderEmail"] = senderEmail,
            ["EmailSettings:SenderPassword"] = senderPassword,
            ["EmailSettings:UseSsl"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();

        return new EmailService(configuration, logger);
    }

    [Fact]
    public async Task EnviarCorreoAsync_ConfiguracionVacia_RegistraElCorreoYDevuelveTrue()
    {
        var logger = new LoggerDePrueba<EmailService>();
        var servicio = CrearServicio(
            logger,
            smtpServer: "",
            senderEmail: "",
            senderPassword: "");

        var enviado = await servicio.EnviarCorreoAsync(
            "nuevo@test.com",
            "Confirmación de cuenta",
            "<p><a href='http://localhost:8080/api/auth/activar?token=abc123'>Activar Cuenta</a></p>");

        Assert.True(enviado);
        Assert.Contains(logger.Mensajes, m => m.Contains("[FALLBACK CORREO]"));
        Assert.Contains(logger.Mensajes, m => m.Contains("nuevo@test.com"));
        Assert.Contains(logger.Mensajes, m => m.Contains("token=abc123"));
    }

    [Fact]
    public async Task EnviarCorreoAsync_ValoresDeEjemplo_RegistraElCorreoYDevuelveTrue()
    {
        var logger = new LoggerDePrueba<EmailService>();
        var servicio = CrearServicio(
            logger,
            smtpServer: "smtp.gmail.com",
            senderEmail: "tu_correo@gmail.com",
            senderPassword: "valor-de-ejemplo");

        var enviado = await servicio.EnviarCorreoAsync(
            "nuevo@test.com",
            "Código de recuperación de contraseña",
            "<p>Tu código de recuperación es: <strong>123456</strong></p>");

        Assert.True(enviado);
        Assert.Contains(logger.Mensajes, m => m.Contains("[FALLBACK CORREO]"));
        Assert.Contains(logger.Mensajes, m => m.Contains("123456"));
    }
}
