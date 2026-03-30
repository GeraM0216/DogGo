using DogGo.Models;

namespace DogGo.ViewModels
{
    public class PaseadorPerfilViewModel
    {
        public Paseador Paseador { get; set; }
        public List<Calificacion> Calificaciones { get; set; }
        public double Promedio { get; set; }
        public int TotalPaseos { get; set; }
    }
}