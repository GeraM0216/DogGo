using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Perro
    {
        public int Id { get; set; }
        public int DueñoId { get; set; }

        [Required] public string Nombre { get; set; }
        public string? Raza { get; set; }
        public int Edad { get; set; }

        /// <summary>"Pequeño", "Mediano", "Grande"</summary>
        public string? Tamaño { get; set; }
        public string? Notas { get; set; }
        //fotitos
        public string? ImagenUrl { get; set; }

        // Navegación
        public Usuario Dueño { get; set; }
        public ICollection<Paseo>? Paseos { get; set; }


 

    }
}