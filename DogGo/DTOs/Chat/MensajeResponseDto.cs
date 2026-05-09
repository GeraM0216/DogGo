namespace DogGo.DTOs.Chat
{
    public class MensajeResponseDto
    {
        public int Id { get; set; }
        public int PaseoId { get; set; }

        public int EmisorId { get; set; }
        public string EmisorNombre { get; set; } = string.Empty;
        public string EmisorApellido { get; set; } = string.Empty;
        public string EmisorNombreCompleto { get; set; } = string.Empty;
        public string EmisorRol { get; set; } = string.Empty;

        public int ReceptorId { get; set; }
        public string ReceptorNombre { get; set; } = string.Empty;
        public string ReceptorApellido { get; set; } = string.Empty;
        public string ReceptorNombreCompleto { get; set; } = string.Empty;
        public string ReceptorRol { get; set; } = string.Empty;

        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
        public bool Leido { get; set; }
        public bool EsMio { get; set; }
    }
}
