namespace DogGo.DTOs.Paseadores
{
    public class PaseadorListItemDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal TarifaPorHora { get; set; }
        public decimal? CalificacionPromedio { get; set; }
        public bool Disponible { get; set; }
        public string? FotoUrl { get; set; }
    }
}