namespace DogGo.DTOs.Perros
{
    public class PerroResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public int Edad { get; set; }
        public decimal? Peso { get; set; }
        public string? Notas { get; set; }
        public string? FotoUrl { get; set; }
    }
}