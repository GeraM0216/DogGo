using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Paseador
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Tarifa por hora")]
        public decimal TarifaPorHora { get; set; }

        public decimal CalificacionPromedio { get; set; }
        public bool Disponible { get; set; } = true;

        public string? FotoUrl { get; set; }

        [Display(Name = "Zona de servicio")]
        public string? ZonaServicio { get; set; }

        [Display(Name = "Años de experiencia")]
        public int? ExperienciaAnios { get; set; }

        // Navegación
        public Usuario Usuario { get; set; } = null!;
        public ICollection<Paseo> Paseos { get; set; } = new List<Paseo>();
    }
}