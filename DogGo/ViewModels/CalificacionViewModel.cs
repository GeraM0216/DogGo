using System.ComponentModel.DataAnnotations;

namespace DogGo.ViewModels
{
    public class CalificacionViewModel
    {
        public int PaseoId { get; set; }

        [Required(ErrorMessage = "Selecciona una puntuación")]
        [Range(1, 5, ErrorMessage = "La puntuación debe ser entre 1 y 5")]
        public int Puntaje { get; set; }

        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string Comentario { get; set; }
    }
}