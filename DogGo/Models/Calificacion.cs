using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Calificacion
    {
        public int Id { get; set; }

        public int PaseoId { get; set; }

        public int DueñoId { get; set; }

        [Range(1, 5)]
        public int Puntaje { get; set; }

        [MaxLength(500)]
        public string? Comentario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // Navegación
        public Paseo Paseo { get; set; } = null!;
        public Usuario Dueño { get; set; } = null!;
    }
}