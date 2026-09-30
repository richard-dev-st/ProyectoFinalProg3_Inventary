namespace Core.Domain.Entities
{
    public class TokenRevocado
    {
        public int Id { get; set; }
        public string TokenJti { get; set; } = string.Empty; // Identificador único del token JWT (JTI)
        public Guid? UsuarioId { get; set; }
        public DateTime FechaRevocacion { get; set; }
        public DateTime FechaExpiracion { get; set; } // Para limpiar tokens expirados de la base de datos
    }
}