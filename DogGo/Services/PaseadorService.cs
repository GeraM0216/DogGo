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
                .ThenBy(p => p.Usuario.Nombre)
                .Select(p => new PaseadorListItemDto
                {
                    Id = p.Id,
                    UsuarioId = p.UsuarioId,

                    Nombre = p.Usuario.Nombre,
                    Apellido = p.Usuario.Apellido,
                    NombreCompleto = (p.Usuario.Nombre + " " + p.Usuario.Apellido).Trim(),
                    Email = p.Usuario.Email,

                    Descripcion = p.Descripcion,
                    TarifaPorHora = p.TarifaPorHora,
                    CalificacionPromedio = p.CalificacionPromedio,
                    Disponible = p.Disponible,

                    FotoUrl = p.FotoUrl,
                    ImagenUrl = p.FotoUrl,

                    ZonaServicio = p.ZonaServicio,
                    ExperienciaAnios = p.ExperienciaAnios
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

                    Nombre = p.Usuario.Nombre,
                    Apellido = p.Usuario.Apellido,
                    NombreCompleto = (p.Usuario.Nombre + " " + p.Usuario.Apellido).Trim(),
                    Email = p.Usuario.Email,

                    Descripcion = p.Descripcion,
                    TarifaPorHora = p.TarifaPorHora,
                    CalificacionPromedio = p.CalificacionPromedio,
                    Disponible = p.Disponible,

                    FotoUrl = p.FotoUrl,
                    ImagenUrl = p.FotoUrl,

                    ZonaServicio = p.ZonaServicio,
                    ExperienciaAnios = p.ExperienciaAnios
                })
                .FirstOrDefaultAsync();
        }
    }
}