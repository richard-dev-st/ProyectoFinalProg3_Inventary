using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.Entities
{
    public class CorreoEnCola
    {
        public Guid Id { get; private set; }
        public string Destinatario { get; private set; }
        public string Asunto { get; private set; }
        public string Cuerpo { get; private set; }
        public string Estado { get; private set; } // Pendiente, Enviado, Fallido
        public DateTime FechaCreacion { get; private set; }
        public DateTime? FechaEnvio { get; private set; }

        private CorreoEnCola() { }

        public CorreoEnCola(string destinatario, string asunto, string cuerpo)
        {
            Id = Guid.NewGuid();
            Destinatario = destinatario;
            Asunto = asunto;
            Cuerpo = cuerpo;
            Estado = "Pendiente";
            FechaCreacion = DateTime.UtcNow;
        }

        // Método para marcar el correo como enviado
        public void MarcarComoEnviado()
        {
            Estado = "Enviado";
            FechaEnvio = DateTime.UtcNow;
        }
    }
}
