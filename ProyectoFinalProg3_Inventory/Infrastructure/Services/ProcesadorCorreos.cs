using Application.Interfaces;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Procesa la cola de correos con un número limitado de reintentos.
    /// Separado del <see cref="ProcesadorCorreosWorker"/> para poder probarlo sin host.
    /// </summary>
    public class ProcesadorCorreos
    {
        /// <summary>RF-NOT-10: máximo de intentos de envío por correo.</summary>
        public const int MaximoIntentos = 3;

        private const int TamanoLote = 10;

        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProcesadorCorreos> _logger;

        public ProcesadorCorreos(
            IApplicationDbContext context,
            IEmailService emailService,
            ILogger<ProcesadorCorreos> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// RF-NOT-09: toma los pendientes y los envía. RF-NOT-12: un correo ya enviado
        /// no vuelve a seleccionarse porque el filtro exige estado Pendiente.
        /// </summary>
        public async Task<int> ProcesarPendientesAsync(CancellationToken cancellationToken = default)
        {
            var pendientes = await _context.CorreosEnCola
                .Where(c => c.Estado == CorreoEnCola.EstadoPendiente
                            && c.Intentos < MaximoIntentos)
                .OrderBy(c => c.FechaCreacion)
                .Take(TamanoLote)
                .ToListAsync(cancellationToken);

            var procesados = 0;

            foreach (var correo in pendientes)
            {
                if (!correo.PuedeProcesarse(MaximoIntentos))
                {
                    continue;
                }

                // Se contabiliza el intento antes de enviar y se guarda el estado tras cada
                // correo: si el proceso muere, el reintento queda registrado (RF-NOT-10).
                correo.RegistrarIntento();

                try
                {
                    var enviado = await _emailService.EnviarCorreoAsync(
                        correo.Destinatario,
                        correo.Asunto,
                        correo.Cuerpo);

                    if (enviado)
                    {
                        correo.MarcarComoEnviado();
                        _logger.LogInformation(
                            "Correo enviado a {Destinatario} (intento {Intento}).",
                            correo.Destinatario,
                            correo.Intentos);
                    }
                    else
                    {
                        correo.RegistrarFallo(
                            _emailService.UltimoError
                                ?? "El servicio de correo no pudo enviar el mensaje.",
                            MaximoIntentos);
                        _logger.LogWarning(
                            "Fallo al enviar el correo a {Destinatario} (intento {Intento}/{Maximo}).",
                            correo.Destinatario,
                            correo.Intentos,
                            MaximoIntentos);
                    }
                }
                catch (Exception ex)
                {
                    correo.RegistrarFallo(ex.Message, MaximoIntentos);
                    _logger.LogError(
                        ex,
                        "Error al enviar el correo a {Destinatario} (intento {Intento}/{Maximo}).",
                        correo.Destinatario,
                        correo.Intentos,
                        MaximoIntentos);
                }

                if (correo.Estado == CorreoEnCola.EstadoFallido)
                {
                    _logger.LogError(
                        "El correo a {Destinatario} quedó Fallido tras {Intentos} intentos. Último error: {Error}",
                        correo.Destinatario,
                        correo.Intentos,
                        correo.UltimoError);
                }

                await _context.SaveChangesAsync(cancellationToken);
                procesados++;
            }

            return procesados;
        }
    }
}
