namespace DogGo.DTOs.Paseos
{
    public class PaseoDetalleDto
    {
        public int Id { get; set; }
        public int PerroId { get; set; }
        public string PerroNombre { get; set; } = string.Empty;
        public int PaseadorId { get; set; }
        public string PaseadorNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; }
        public bool EsProgramado { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal Precio { get; set; }
        public string? MotivoCancelacion { get; set; }
        public string? CanceladoPor { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public string? FotoInicioUrl { get; set; }
        public string? FotoFinUrl { get; set; }
    }
}