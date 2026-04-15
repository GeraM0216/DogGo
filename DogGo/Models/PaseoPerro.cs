namespace DogGo.Models
{
    public class PaseoPerro
    {
        public int Id { get; set; }

        public int PaseoId { get; set; }
        public Paseo Paseo { get; set; } = null!;

        public int PerroId { get; set; }
        public Perro Perro { get; set; } = null!;
    }
}