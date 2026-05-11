using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(120)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telefono { get; set; } = string.Empty;

        public bool EmailConfirmado { get; set; } = false;

        [MaxLength(10)]
        public string? CodigoConfirmacion { get; set; }

        public DateTime? CodigoExpiraEn { get; set; }

        [MaxLength(10)]
        public string? CodigoRecuperacion { get; set; }

        public DateTime? CodigoRecuperacionExpiraEn { get; set; }

        /// <summary>"Duenio", "Paseador", "Admin" o "SuperAdmin"</summary>
        [Required]
        [MaxLength(20)]
        public string Rol { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;

        public DateTime? FechaDesactivacion { get; set; }

        [MaxLength(300)]
        public string? MotivoDesactivacion { get; set; }

        // Navegación
        public ICollection<Perro> Perros { get; set; } = new List<Perro>();
        public Paseador? Paseador { get; set; }
        public DuenioPerfil? DuenioPerfil { get; set; }
        public ICollection<Mensaje> Enviados { get; set; } = new List<Mensaje>();
        public ICollection<Mensaje> Recibidos { get; set; } = new List<Mensaje>();
    }
}