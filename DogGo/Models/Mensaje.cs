using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Mensaje
    {
        public int Id { get; set; }

        public int EmisorId { get; set; }
        public int ReceptorId { get; set; }
        public int PaseoId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Contenido { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

        public bool Leido { get; set; } = false;

        // Navegación
        public Usuario Emisor { get; set; } = null!;
        public Usuario Receptor { get; set; } = null!;
        public Paseo Paseo { get; set; } = null!;
    }
}
