using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Perro
    {
        public int Id { get; set; }
        public int DueñoId { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        public string? Raza { get; set; }
        public int Edad { get; set; }

        /// <summary>"Pequeño", "Mediano", "Grande"</summary>
        public string? Tamaño { get; set; }
        public string? Notas { get; set; }
        public string? ImagenUrl { get; set; }

        // Navegación actual
        public Usuario Dueño { get; set; } = null!;
        public ICollection<Paseo>? Paseos { get; set; }

        // Nueva relación para múltiples paseos/perros
        public ICollection<PaseoPerro> PaseoPerros { get; set; } = new List<PaseoPerro>();
    }
}