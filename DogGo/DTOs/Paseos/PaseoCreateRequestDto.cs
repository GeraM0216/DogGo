using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DogGo.DTOs.Paseos
{
    public class PaseoCreateRequestDto
    {
        [Required(ErrorMessage = "El perro es obligatorio.")]
        public int PerroId { get; set; }

        [Required(ErrorMessage = "El paseador es obligatorio.")]
        public int PaseadorId { get; set; }

        [Range(1, 1440, ErrorMessage = "La duración debe estar entre 1 y 1440 minutos.")]
        public int DuracionMinutos { get; set; }

        public bool EsProgramado { get; set; }

        public DateTime? FechaProgramada { get; set; }

        [Range(0, 100000, ErrorMessage = "El precio debe estar entre 0 y 100000.")]
        public decimal Precio { get; set; }

        // Ubicación de recogida enviada desde Flutter
        public decimal? LatitudRecogida { get; set; }
        public decimal? LongitudRecogida { get; set; }

        [MaxLength(200, ErrorMessage = "La ubicación no puede superar 200 caracteres.")]
        public string? UbicacionTexto { get; set; }

        [MaxLength(200, ErrorMessage = "La dirección de recogida no puede superar 200 caracteres.")]
        public string? DireccionRecogida { get; set; }

        [MaxLength(300, ErrorMessage = "Las referencias no pueden superar 300 caracteres.")]
        public string? ReferenciasRecogida { get; set; }

        [MaxLength(100, ErrorMessage = "La zona no puede superar 100 caracteres.")]
        public string? ZonaRecogida { get; set; }

        // Notas opcionales del dueño para el paseador
        [MaxLength(300, ErrorMessage = "Las notas no pueden superar 300 caracteres.")]
        public string? Notas { get; set; }

        [MaxLength(300, ErrorMessage = "Las observaciones no pueden superar 300 caracteres.")]
        public string? Observaciones { get; set; }

        // Compatibilidad si Flutter manda nombres alternativos
        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("referencias")]
        public string? Referencias { get; set; }
    }
}
