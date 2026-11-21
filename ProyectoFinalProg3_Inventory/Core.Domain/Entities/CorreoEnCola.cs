namespace Core.Domain.Entities
{
    public class CorreoEnCola
    {
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoEnviado = "Enviado";
        public const string EstadoFallido = "Fallido";

        private const int LongitudMaximaError = 1000;

        public Guid Id { get; private set; }
        public string Destinatario { get; private set; } = string.Empty;
        public string Asunto { get; private set; } = string.Empty;
        public string Cuerpo { get; private set; } = string.Empty;
        public string Estado { get; private set; } = EstadoPendiente;
        public DateTime FechaCreacion { get; private set; }
        public DateTime? FechaEnvio { get; private set; }

        // RF-NOT-10/11: trazabilidad de reintentos y del último error.
        public int Intentos { get; private set; }
        public string? UltimoError { get; private set; }
        public DateTime? FechaUltimoIntento { get; private set; }

        private CorreoEnCola() { }

        public CorreoEnCola(string destinatario, string asunto, string cuerpo)
        {
            Id = Guid.NewGuid();
            Destinatario = destinatario;
            Asunto = asunto;
            Cuerpo = cuerpo;
            Estado = EstadoPendiente;
            FechaCreacion = DateTime.UtcNow;
        }

        /// <summary>RF-NOT-09: cuenta el intento antes de hablar con el servidor SMTP.</summary>
        public void RegistrarIntento()
        {
            Intentos++;
            FechaUltimoIntento = DateTime.UtcNow;
        }

        /// <summary>RF-NOT-12: marcar como enviado es idempotente.</summary>
        public void MarcarComoEnviado()
        {
            if (Estado == EstadoEnviado)
            {
                return;
            }

            Estado = EstadoEnviado;
            FechaEnvio = DateTime.UtcNow;
            UltimoError = null;
        }

        /// <summary>
        /// RF-NOT-10: guarda el error y pasa a Fallido al alcanzar el máximo de intentos.
        /// </summary>
        public void RegistrarFallo(string error, int maximoIntentos)
        {
            UltimoError = string.IsNullOrWhiteSpace(error)
                ? "Error desconocido al enviar el correo."
                : error[..Math.Min(error.Length, LongitudMaximaError)];
            FechaUltimoIntento = DateTime.UtcNow;

            if (Intentos >= maximoIntentos)
            {
                Estado = EstadoFallido;
            }
        }

        /// <summary>Un correo solo se procesa si sigue pendiente y no agotó sus intentos.</summary>
        public bool PuedeProcesarse(int maximoIntentos)
        {
            return Estado == EstadoPendiente && Intentos < maximoIntentos;
        }
    }
}
