using System.ComponentModel.DataAnnotations;

namespace DogGo.Models
{
    public class Paseo
    {
        public int Id { get; set; }

        public int PaseadorId { get; set; }

        // Se mantiene temporalmente por compatibilidad con lo actual
        public int PerroId { get; set; }

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        /// <summary>"Pendiente", "Aceptado", "EnCurso", "Finalizado", "Cancelado"</summary>
        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = "Pendiente";

        public decimal LatitudActual { get; set; }
        public decimal LongitudActual { get; set; }

        [Range(0, 100000)]
        public decimal Precio { get; set; }

        [Range(1, 1440)]
        public int DuracionMinutos { get; set; }

        // Duración real final del paseo. Se calcula al finalizar.
        [Range(0, 1440)]
        public int? DuracionRealMinutos { get; set; }

        public bool EsProgramado { get; set; } = false;
        public DateTime? FechaProgramada { get; set; }

        // Ubicación de recolección del perro para 1 paseo
        [MaxLength(200)]
        public string? DireccionRecogida { get; set; }

        [MaxLength(300)]
        public string? ReferenciasRecogida { get; set; }

        [MaxLength(100)]
        public string? ZonaRecogida { get; set; }

        public decimal? LatitudRecogida { get; set; }
        public decimal? LongitudRecogida { get; set; }

        [MaxLength(300)]
        public string? MotivoCancelacion { get; set; }

        [MaxLength(20)]
        public string? CanceladoPor { get; set; }

        public DateTime? FechaCancelacion { get; set; }

        // Guardar solo ruta/URL corta, no base64.
        [MaxLength(500)]
        public string? FotoInicioUrl { get; set; }

        [MaxLength(500)]
        public string? FotoFinUrl { get; set; }

        // Finalización anticipada
        public bool FinalizacionAnticipadaSolicitada { get; set; } = false;

        [MaxLength(300)]
        public string? MotivoFinalizacionAnticipada { get; set; }

        public DateTime? FechaSolicitudFinalizacionAnticipada { get; set; }
        public bool? FinalizacionAnticipadaAprobada { get; set; }
        public DateTime? FechaRespuestaFinalizacionAnticipada { get; set; }

        // Navegación actual
        public Paseador Paseador { get; set; } = null!;
        public Perro Perro { get; set; } = null!;
        public Calificacion? Calificacion { get; set; }
        public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
        public ICollection<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();

        // Nueva relación para múltiples perros
        public ICollection<PaseoPerro> PaseoPerros { get; set; } = new List<PaseoPerro>();
    }
}
