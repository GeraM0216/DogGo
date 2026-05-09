using System.ComponentModel.DataAnnotations;

namespace DogGo.DTOs.Chat
{
    public class EnviarMensajeRequestDto
    {
        public int? PaseoId { get; set; }

        // Opcional. Si no se manda, el backend calcula el receptor:
        // Dueño -> paseador asignado
        // Paseador -> dueño del perro
        public int? ReceptorId { get; set; }

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        [MaxLength(1000, ErrorMessage = "El mensaje no puede superar 1000 caracteres.")]
        public string Contenido { get; set; } = string.Empty;
    }
}