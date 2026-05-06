using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Paseador
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        [Display(Name = "Descripción")]
        [MaxLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Tarifa por hora")]
        [Range(0, 10000)]
        public decimal TarifaPorHora { get; set; }

        [Range(0, 5)]
        public decimal CalificacionPromedio { get; set; }

        public bool Disponible { get; set; } = true;

        // Guardar solo ruta/URL corta. Ejemplo: /uploads/paseadores/paseador-1.jpg
        [MaxLength(500)]
        public string? FotoUrl { get; set; }

        [Display(Name = "Zona de servicio")]
        [MaxLength(100)]
        public string? ZonaServicio { get; set; }

        [Display(Name = "Años de experiencia")]
        [Range(0, 60)]
        public int? ExperienciaAnios { get; set; }

        // Navegación
        public Usuario Usuario { get; set; } = null!;
        public ICollection<Paseo> Paseos { get; set; } = new List<Paseo>();
    }
}
