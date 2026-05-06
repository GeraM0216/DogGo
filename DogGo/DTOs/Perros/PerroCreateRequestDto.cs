using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DogGo.DTOs.Perros
{
    public class PerroCreateRequestDto
    {
        [Required(ErrorMessage = "El nombre del perro es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "La raza no puede superar 50 caracteres.")]
        public string Raza { get; set; } = string.Empty;

        [Range(0, 30, ErrorMessage = "La edad debe estar entre 0 y 30 años.")]
        public int Edad { get; set; }

        [MaxLength(20, ErrorMessage = "El tamaño no puede superar 20 caracteres.")]
        public string? Tamano { get; set; }

        [JsonPropertyName("tamanio")]
        [MaxLength(20, ErrorMessage = "El tamaño no puede superar 20 caracteres.")]
        public string? Tamanio { get; set; }

        [JsonPropertyName("tamaño")]
        [MaxLength(20, ErrorMessage = "El tamaño no puede superar 20 caracteres.")]
        public string? Tamaño { get; set; }

        public decimal? Peso { get; set; }

        [MaxLength(300, ErrorMessage = "Las notas no pueden superar 300 caracteres.")]
        public string? Notas { get; set; }

        [MaxLength(300, ErrorMessage = "Las observaciones no pueden superar 300 caracteres.")]
        public string? Observaciones { get; set; }

        [MaxLength(500, ErrorMessage = "La URL de imagen no puede superar 500 caracteres.")]
        public string? ImagenUrl { get; set; }

        [MaxLength(500, ErrorMessage = "La URL de foto no puede superar 500 caracteres.")]
        public string? FotoUrl { get; set; }
    }
}
