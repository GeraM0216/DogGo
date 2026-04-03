using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DogGo.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required] public string Nombre { get; set; }
        [Required] public string Apellido { get; set; }
        [Required] public string Email { get; set; }
        [Required] public string PasswordHash { get; set; }
        public string Telefono { get; set; }

        public bool EmailConfirmado { get; set; } = false;
        public string? CodigoConfirmacion { get; set; }
        public DateTime? CodigoExpiraEn { get; set; }

        /// <summary>"Duenio" o "Paseador"</summary>
        [Required] public string Rol { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<Perro> Perros { get; set; }
        public Paseador Paseador { get; set; }
        public ICollection<Mensaje> Enviados { get; set; }
        public ICollection<Mensaje> Recibidos { get; set; }
    }
}