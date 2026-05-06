using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Perro
    {
        public int Id { get; set; }

        public int DueñoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = null!;

        [MaxLength(50)]
        public string? Raza { get; set; }

        [Range(0, 30)]
        public int Edad { get; set; }

        /// <summary>"Pequeño", "Mediano", "Grande"</summary>
        [MaxLength(20)]
        public string? Tamaño { get; set; }

        [MaxLength(300)]
        public string? Notas { get; set; }

        // Guardar SOLO la ruta/URL, no base64 ni imagen pesada.
        // Ejemplo: /uploads/perros/perro-1-abc.jpg
        [MaxLength(500)]
        public string? ImagenUrl { get; set; }

        public Usuario Dueño { get; set; } = null!;
        public ICollection<Paseo>? Paseos { get; set; }
        public ICollection<PaseoPerro> PaseoPerros { get; set; } = new List<PaseoPerro>();
    }
}