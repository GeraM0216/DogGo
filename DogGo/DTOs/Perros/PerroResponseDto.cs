namespace DogGo.DTOs.Perros
{
    public class PerroResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public int Edad { get; set; }

        public string? Tamano { get; set; }
        public string? Tamanio { get; set; }
        public string? Tamaño { get; set; }

        public decimal? Peso { get; set; }
        public string? Notas { get; set; }

        // Flutter puede leer FotoUrl o ImagenUrl.
        public string? FotoUrl { get; set; }
        public string? ImagenUrl { get; set; }
    }
}