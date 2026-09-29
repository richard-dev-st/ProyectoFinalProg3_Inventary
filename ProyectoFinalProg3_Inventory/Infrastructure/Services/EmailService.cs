using Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = int.TryParse(_configuration["EmailSettings:SmtpPort"], out int p) ? p : 587; // Valor predeterminado si no se puede analizar
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];

                using var message = new MailMessage();
                message.From = new MailAddress(senderEmail!, "Sistema de Inventario");
                message.To.Add(new MailAddress(destinatario));
                message.Subject = asunto;
                message.Body = cuerpoHtml;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpServer, port);

                client.UseDefaultCredentials = false; //Evita usar las credenciales predeterminadas del sistema
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                client.EnableSsl = true;
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