using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DogGo.DTOs.Auth
{
    public class ChangePasswordRequestDto
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña actual debe tener al menos 6 caracteres.")]
        [MaxLength(100, ErrorMessage = "La contraseña actual no puede superar 100 caracteres.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [JsonPropertyName("passwordActual")]
        public string? PasswordActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        [MaxLength(100, ErrorMessage = "La nueva contraseña no puede superar 100 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        [JsonPropertyName("passwordNueva")]
        public string? PasswordNueva { get; set; }

        [JsonPropertyName("nuevaPassword")]
        public string? NuevaPassword { get; set; }
    }
}
