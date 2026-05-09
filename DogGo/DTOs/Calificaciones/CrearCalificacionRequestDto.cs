using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Calificaciones
{
    public class CrearCalificacionRequestDto
    {
        public int? PaseoId { get; set; }

        [Required(ErrorMessage = "El puntaje es obligatorio.")]
        [Range(1, 5, ErrorMessage = "El puntaje debe estar entre 1 y 5.")]
        public int Puntaje { get; set; }

        [MaxLength(500, ErrorMessage = "El comentario no puede superar 500 caracteres.")]
        public string? Comentario { get; set; }
    }
}