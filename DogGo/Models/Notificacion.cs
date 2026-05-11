using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(120)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Tipo { get; set; } = "General";

        public int? ReferenciaId { get; set; }

        public bool Leida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public Usuario Usuario { get; set; } = null!;
    }
}