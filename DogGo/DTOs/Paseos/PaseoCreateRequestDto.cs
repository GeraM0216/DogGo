namespace DogGo.DTOs.Paseos
{
    public class PaseoCreateRequestDto
    {
        public int PerroId { get; set; }
        public int PaseadorId { get; set; }
        public int DuracionMinutos { get; set; }
        public bool EsProgramado { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public decimal Precio { get; set; }

        // Ubicación de recogida enviada desde Flutter
        public decimal? LatitudRecogida { get; set; }
        public decimal? LongitudRecogida { get; set; }
        public string? UbicacionTexto { get; set; }
        public string? DireccionRecogida { get; set; }
        public string? ReferenciasRecogida { get; set; }
        public string? ZonaRecogida { get; set; }

        // Notas opcionales del dueño para el paseador
        public string? Notas { get; set; }
        public string? Observaciones { get; set; }
    }
}
