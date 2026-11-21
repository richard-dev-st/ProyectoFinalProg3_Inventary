namespace Core.Application.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// RF-NOT-10: mensaje de la última excepción del envío. Es <c>null</c> si el último
        /// intento no falló. El procesador lo guarda en la cola como <c>UltimoError</c>.
        /// </summary>
        string? UltimoError { get; }

        Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml);
    }
}
