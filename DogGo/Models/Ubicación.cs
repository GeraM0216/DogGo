namespace DogGo.Models
{
    public class Ubicacion
    {
        public int Id { get; set; }

        public int PaseoId { get; set; }

        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navegación
        public Paseo Paseo { get; set; } = null!;
    }
}
