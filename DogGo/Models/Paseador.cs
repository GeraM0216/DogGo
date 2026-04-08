using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogGo.Models
{
    public class Paseador
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Descripcion { get; set; }
        public decimal TarifaPorHora { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public bool Disponible { get; set; } = true;

        public string? FotoUrl { get; set; }
        public string? ZonaServicio { get; set; }
        public int? ExperienciaAnios { get; set; }

        // Navegación
        public Usuario Usuario { get; set; }
        public ICollection<Paseo> Paseos { get; set; }
    }
}