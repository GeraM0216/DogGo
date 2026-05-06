using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código es obligatorio.")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "El código debe tener 6 dígitos.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        [MaxLength(100, ErrorMessage = "La nueva contraseña no puede superar 100 caracteres.")]
        public string NuevaPassword { get; set; } = string.Empty;
    }
}
