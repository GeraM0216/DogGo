namespace DogGo.Models
{
    public class Mensaje
    {
        public int Id { get; set; }
        public int EmisorId { get; set; }
        public int ReceptorId { get; set; }
        public int PaseoId { get; set; }
        public string Contenido { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
        public bool Leido { get; set; } = false;

        // Navegación
        public Usuario Emisor { get; set; }
        public Usuario Receptor { get; set; }
        public Paseo Paseo { get; set; }
    }
}