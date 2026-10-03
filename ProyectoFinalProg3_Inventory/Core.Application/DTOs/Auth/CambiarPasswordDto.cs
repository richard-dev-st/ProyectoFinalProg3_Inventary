using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public record CambiarPasswordDto
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }
}
