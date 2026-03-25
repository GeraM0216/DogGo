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

        // Navegación
        public Usuario Usuario { get; set; }
        public ICollection<Paseo> Paseos { get; set; }
    }
}