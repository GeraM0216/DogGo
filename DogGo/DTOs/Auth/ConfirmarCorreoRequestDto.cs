namespace DogGo.DTOs.Auth
{
    public class ConfirmarCorreoRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
    }
}