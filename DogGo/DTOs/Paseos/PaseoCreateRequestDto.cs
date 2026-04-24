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
    }
}