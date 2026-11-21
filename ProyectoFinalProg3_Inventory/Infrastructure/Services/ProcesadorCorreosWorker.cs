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
                    using var scope = _serviceProvider.CreateScope();
                    var procesador = scope.ServiceProvider.GetRequiredService<ProcesadorCorreos>();

                    var procesados = await procesador.ProcesarPendientesAsync(stoppingToken);

                    if (procesados > 0)
                    {
                        _logger.LogInformation("Se procesaron {Cantidad} correo(s) de la cola.", procesados);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocurrió un error al procesar la cola de correos.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
