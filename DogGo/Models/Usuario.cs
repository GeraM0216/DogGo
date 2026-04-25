using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public bool EmailConfirmado { get; set; } = false;
        public string? CodigoConfirmacion { get; set; }
        public DateTime? CodigoExpiraEn { get; set; }

        public string? CodigoRecuperacion { get; set; }
        public DateTime? CodigoRecuperacionExpiraEn { get; set; }

        /// <summary>"Duenio" o "Paseador"</summary>
        [Required]
        public string Rol { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<Perro> Perros { get; set; } = new List<Perro>();
        public Paseador? Paseador { get; set; }
        public DuenioPerfil? DuenioPerfil { get; set; }
        public ICollection<Mensaje> Enviados { get; set; } = new List<Mensaje>();
        public ICollection<Mensaje> Recibidos { get; set; } = new List<Mensaje>();
    }
}