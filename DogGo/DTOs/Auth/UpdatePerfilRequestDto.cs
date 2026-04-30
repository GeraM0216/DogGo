namespace DogGo.DTOs.Auth
{
    public class UpdatePerfilRequestDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Telefono { get; set; }
    }
}