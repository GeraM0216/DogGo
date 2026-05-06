namespace DogGo.DTOs.Paseadores
{
    public class PaseadorDetalleDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string? Descripcion { get; set; }
        public decimal TarifaPorHora { get; set; }
        public decimal? CalificacionPromedio { get; set; }
        public bool Disponible { get; set; }

        public string? FotoUrl { get; set; }
        public string? ImagenUrl { get; set; }

        public string? ZonaServicio { get; set; }
        public int? ExperienciaAnios { get; set; }
    }
}
