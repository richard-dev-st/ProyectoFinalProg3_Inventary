using Application.Interfaces;
using Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class ProcesadorCorreosWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProcesadorCorreosWorker> _logger;

        public ProcesadorCorreosWorker(IServiceProvider serviceProvider, ILogger<ProcesadorCorreosWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("El servicio en segundo plano ProcesadorCorreosWorker ha iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Lógica para procesar la cola de correos

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        var correosPendientes = await context.CorreosEnCola
                            .Where(c => c.Estado == "Pendiente")
                            .Take(10)
                            .ToListAsync(stoppingToken);

                        if (correosPendientes.Any())
                        {
                            foreach (var correo in correosPendientes)
                            {
                                bool enviado = await emailService.EnviarCorreoAsync(
                                    correo.Destinatario,
                                    correo.Asunto,
                                    correo.Cuerpo
                                    );

                                if (enviado)
                                {
                                    correo.MarcarComoEnviado();
                                }
                            }

                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocurrió un error al procesar la cola de correos.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Espera 5 segundos antes de la siguiente iteración

            }
        }
    }
}