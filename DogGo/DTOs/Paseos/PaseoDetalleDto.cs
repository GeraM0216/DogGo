namespace DogGo.DTOs.Paseos
{
    public class PaseoDetalleDto
    {
        public int Id { get; set; }

        public int PerroId { get; set; }
        public string PerroNombre { get; set; } = string.Empty;
        public string? PerroFotoUrl { get; set; }
        public string? PerroImagenUrl { get; set; }

        public int PaseadorId { get; set; }
        public string PaseadorNombre { get; set; } = string.Empty;
        public string PaseadorApellido { get; set; } = string.Empty;
        public string PaseadorNombreCompleto { get; set; } = string.Empty;
        public string? PaseadorFotoUrl { get; set; }

        public int DuenioId { get; set; }
        public string DuenioNombre { get; set; } = string.Empty;
        public string DuenioApellido { get; set; } = string.Empty;
        public string DuenioNombreCompleto { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public int DuracionMinutos { get; set; }
        public int? DuracionRealMinutos { get; set; }

        public bool EsProgramado { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public decimal Precio { get; set; }

        public string? DireccionRecogida { get; set; }
        public string? UbicacionTexto { get; set; }
        public string? ReferenciasRecogida { get; set; }
        public string? ZonaRecogida { get; set; }
        public decimal? LatitudRecogida { get; set; }
        public decimal? LongitudRecogida { get; set; }

        public decimal LatitudActual { get; set; }
        public decimal LongitudActual { get; set; }

        public string? MotivoCancelacion { get; set; }
        public string? CanceladoPor { get; set; }
        public DateTime? FechaCancelacion { get; set; }

        public string? FotoInicioUrl { get; set; }
        public string? FotoFinUrl { get; set; }
    }
}
