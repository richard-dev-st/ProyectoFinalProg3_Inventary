using System.ComponentModel.DataAnnotations;

namespace Inventario.Web.Models
{
    public class ReenviarActivacionRequest
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;
    }
}
