using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public class CrearSolicitudRequest
    {
        // RF-GP-01: los límites espejean la entidad SolicitudPermiso del backend.
        [Required(ErrorMessage = "El permiso solicitado es obligatorio.")]
        [StringLength(100, ErrorMessage = "El permiso no puede superar los 100 caracteres.")]
        public string Permiso { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;
    }
}
