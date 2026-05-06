using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [MaxLength(100, ErrorMessage = "La contraseña no puede superar 100 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}