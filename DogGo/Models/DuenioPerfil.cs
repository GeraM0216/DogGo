using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class DuenioPerfil
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public string? FotoUrl { get; set; }

        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [Display(Name = "Referencias de dirección")]
        public string? ReferenciasDireccion { get; set; }

        [Display(Name = "Zona")]
        public string? Zona { get; set; }

        public decimal? Latitud { get; set; }

        public decimal? Longitud { get; set; }

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Preferencias de paseo")]
        public string? PreferenciasPaseo { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}