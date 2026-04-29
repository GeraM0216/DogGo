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

        /// <summary>"Pendiente", "EnCurso", "Finalizado", "Cancelado"</summary>
        public string Estado { get; set; } = "Pendiente";

        public decimal LatitudActual { get; set; }
        public decimal LongitudActual { get; set; }
        public decimal Precio { get; set; }

        public int DuracionMinutos { get; set; }

        // Duración real final del paseo. Se calcula al finalizar.
        public int? DuracionRealMinutos { get; set; }

        public bool EsProgramado { get; set; } = false;
        public DateTime? FechaProgramada { get; set; }

        // Ubicación de recolección del perro para 1 paseo
        public string? DireccionRecogida { get; set; }
        public string? ReferenciasRecogida { get; set; }
        public string? ZonaRecogida { get; set; }

        public decimal? LatitudRecogida { get; set; }
        public decimal? LongitudRecogida { get; set; }

        public string? MotivoCancelacion { get; set; }
        public string? CanceladoPor { get; set; }
        public DateTime? FechaCancelacion { get; set; }

        public string? FotoInicioUrl { get; set; }
        public string? FotoFinUrl { get; set; }

        // Finalización anticipada
        public bool FinalizacionAnticipadaSolicitada { get; set; } = false;
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