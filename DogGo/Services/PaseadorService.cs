using DogGo.Data;
using DogGo.DTOs.Paseadores;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Services
{
    public class PaseadorService
    {
        private readonly DogGoDbContext _context;

        public PaseadorService(DogGoDbContext context)
        {
            _context = context;
        }

        public async Task<List<PaseadorListItemDto>> ObtenerTodosAsync()
        {
            return await _context.Paseadores
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.CalificacionPromedio)
                .Select(p => new PaseadorListItemDto
                {
                    Id = p.Id,
                    UsuarioId = p.UsuarioId,
                    NombreCompleto = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    Descripcion = p.Descripcion,
                    TarifaPorHora = p.TarifaPorHora,
                    CalificacionPromedio = p.CalificacionPromedio,
                    Disponible = p.Disponible,
                    FotoUrl = p.FotoUrl
                })
                .ToListAsync();
        }

        public async Task<PaseadorDetalleDto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Paseadores
                .Include(p => p.Usuario)
                .Where(p => p.Id == id)
                .Select(p => new PaseadorDetalleDto
                {
                    Id = p.Id,
                    UsuarioId = p.UsuarioId,
                    NombreCompleto = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    Descripcion = p.Descripcion,
                    TarifaPorHora = p.TarifaPorHora,
                    CalificacionPromedio = p.CalificacionPromedio,
                    Disponible = p.Disponible,
                    FotoUrl = p.FotoUrl,
                    ExperienciaAnios = p.ExperienciaAnios
                })
                .FirstOrDefaultAsync();
        }
    }
}