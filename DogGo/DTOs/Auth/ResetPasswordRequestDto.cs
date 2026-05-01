using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NuevaPassword { get; set; } = string.Empty;
    }
}