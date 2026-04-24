namespace DogGo.DTOs.Paseos
{
    public class PaseoResponseDto
    {
        public int Id { get; set; }
        public int PerroId { get; set; }
        public int PaseadorId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; }
        public bool EsProgramado { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal Precio { get; set; }
    }
}