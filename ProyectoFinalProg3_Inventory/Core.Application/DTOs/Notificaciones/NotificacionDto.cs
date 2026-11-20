namespace Application.DTOs.Notificaciones
{
    /// <summary>Vista de una notificación propia (RF-NOT-04).</summary>
    public class NotificacionDto
    {
        public Guid Id { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? ReferenciaId { get; set; }
    }
}
