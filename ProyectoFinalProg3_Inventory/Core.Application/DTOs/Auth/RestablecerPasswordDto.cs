using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public record RestablecerPasswordDto
    {
        // Dto para recibir la peticion de restablecimiento de contraseña
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }
}
