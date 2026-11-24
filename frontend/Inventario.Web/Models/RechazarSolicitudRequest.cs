using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public class RechazarSolicitudRequest
    {
        // RF-GP-04: sin motivo el backend rechaza la operación.
        [Required(ErrorMessage = "El motivo de rechazo es obligatorio.")]
        [StringLength(500, ErrorMessage = "El motivo de rechazo no puede superar los 500 caracteres.")]
        public string Motivo { get; set; } = string.Empty;
    }
}
