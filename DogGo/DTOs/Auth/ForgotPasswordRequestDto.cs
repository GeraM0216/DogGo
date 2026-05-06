using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
        public string Email { get; set; } = string.Empty;
    }
}