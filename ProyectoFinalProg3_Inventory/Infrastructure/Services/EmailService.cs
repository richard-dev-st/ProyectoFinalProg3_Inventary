using Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        // Valores de ejemplo de .env.example / appsettings.json. Si el correo real no se
        // configuro, el worker no puede enviar nada y la revision manual se queda sin enlace.
        private static readonly string[] ValoresDeEjemplo =
        {
            "tu_correo@gmail.com",
            "TU_PASSWORD_DE_APLICACION",
            "valor-de-ejemplo",
            "cambia-esta-clave"
        };

        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private static bool EsValorDeEjemplo(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return true;
            }

            return ValoresDeEjemplo.Any(v => string.Equals(valor.Trim(), v, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];

            if (EsValorDeEjemplo(smtpServer) || EsValorDeEjemplo(senderEmail) || EsValorDeEjemplo(senderPassword))
            {
                // Fallback de revision: sin SMTP real el correo se escribe en el log del worker
                // y se marca como enviado para no reintentar indefinidamente.
                _logger.LogWarning(
                    "[FALLBACK CORREO] SMTP sin configurar; el correo no se envio realmente. Destinatario: {Destinatario} | Asunto: {Asunto} | Cuerpo: {Cuerpo}",
                    destinatario,
                    asunto,
                    cuerpoHtml);
                return true;
            }

            try
            {
                var port = int.TryParse(_configuration["EmailSettings:SmtpPort"], out int p) ? p : 587; // Valor predeterminado si no se puede analizar

                var useSsl = bool.TryParse(_configuration["EmailSettings:UseSsl"], out var ssl) ? ssl: true;

                using var message = new MailMessage();
                message.From = new MailAddress(senderEmail!, "Sistema de Inventario");
                message.To.Add(new MailAddress(destinatario));
                message.Subject = asunto;
                message.Body = cuerpoHtml;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpServer, port);

                client.UseDefaultCredentials = false; //Evita usar las credenciales predeterminadas del sistema
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                client.EnableSsl = useSsl; // Usa la configuración de UseSsl desde appsettings.json
                client.DeliveryMethod = SmtpDeliveryMethod.Network; // Asegura que se use el método de entrega de red

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar enviar el correo a {Destinatario}", destinatario);
                return false;
            }
        }
    }
}