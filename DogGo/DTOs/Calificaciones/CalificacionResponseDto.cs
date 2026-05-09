namespace DogGo.DTOs.Calificaciones
{
    public class CalificacionResponseDto
    {
        public int Id { get; set; }

        public int PaseoId { get; set; }

        public int DuenioId { get; set; }
        public string DuenioNombre { get; set; } = string.Empty;
        public string DuenioApellido { get; set; } = string.Empty;
        public string DuenioNombreCompleto { get; set; } = string.Empty;

        public int PaseadorId { get; set; }
        public string PaseadorNombre { get; set; } = string.Empty;
        public string PaseadorApellido { get; set; } = string.Empty;
        public string PaseadorNombreCompleto { get; set; } = string.Empty;

        public int Puntaje { get; set; }
        public string? Comentario { get; set; }

        public DateTime Fecha { get; set; }

        public decimal CalificacionPromedioPaseador { get; set; }
    }
}