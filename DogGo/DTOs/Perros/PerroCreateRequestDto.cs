namespace DogGo.DTOs.Perros
{
    public class PerroCreateRequestDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public int Edad { get; set; }
        public decimal? Peso { get; set; }
        public string? Notas { get; set; }
    }
}