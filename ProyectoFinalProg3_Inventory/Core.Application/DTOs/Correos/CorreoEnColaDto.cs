namespace Application.DTOs.Correos
{
    /// <summary>Estado de un correo de la cola (RF-NOT-11).</summary>
    public class CorreoEnColaDto
    {
        public Guid Id { get; set; }
        public string Destinatario { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int Intentos { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaUltimoIntento { get; set; }
        public string? UltimoError { get; set; }
    }
}
