namespace DogGo.Models
{
    public class Paseo
    {
        public int Id { get; set; }
        public int PaseadorId { get; set; }
        public int PerroId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        /// <summary>"Pendiente", "EnCurso", "Finalizado", "Cancelado"</summary>
        public string Estado { get; set; } = "Pendiente";
        public decimal LatitudActual { get; set; }
        public decimal LongitudActual { get; set; }
        public decimal Precio { get; set; }

        // Navegación
        public Paseador Paseador { get; set; }
        public Perro Perro { get; set; }
        public Calificacion Calificacion { get; set; }
        public ICollection<Mensaje> Mensajes { get; set; }
        public ICollection<Ubicacion> Ubicaciones { get; set; }
    }
}