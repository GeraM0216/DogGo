using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Auth
{
    public class UpdatePerfilRequestDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El apellido no puede superar 50 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres.")]
        public string? Telefono { get; set; }
    }
}