using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El apellido no puede superar 50 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [MaxLength(100, ErrorMessage = "La contraseña no puede superar 100 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [MaxLength(20, ErrorMessage = "El rol no puede superar 20 caracteres.")]
        public string Rol { get; set; } = string.Empty;
    }
}
