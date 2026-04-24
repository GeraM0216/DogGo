using DogGo.Data;
using DogGo.DTOs.Perros;
using DogGo.Models;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Services
{
    public class PerroService
    {
        private readonly DogGoDbContext _context;

        public PerroService(DogGoDbContext context)
        {
            _context = context;
        }

        public async Task<List<PerroResponseDto>> ObtenerMisPerrosAsync(int usuarioId)
        {
            return await _context.Perros
                .Where(p => p.DueñoId == usuarioId)
                .Select(p => new PerroResponseDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Raza = p.Raza,
                    Edad = p.Edad
                })
                .ToListAsync();
        }

        public async Task<PerroResponseDto?> ObtenerPorIdAsync(int perroId, int usuarioId)
        {
            return await _context.Perros
                .Where(p => p.Id == perroId && p.DueñoId == usuarioId)
                .Select(p => new PerroResponseDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Raza = p.Raza,
                    Edad = p.Edad
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, PerroResponseDto? Data)> CrearAsync(int usuarioId, PerroCreateRequestDto dto)
        {
            var perro = new Perro
            {
                Nombre = dto.Nombre,
                Raza = dto.Raza,
                Edad = dto.Edad,
                DueñoId = usuarioId
            };

            _context.Perros.Add(perro);
            await _context.SaveChangesAsync();

            var response = new PerroResponseDto
            {
                Id = perro.Id,
                Nombre = perro.Nombre,
                Raza = perro.Raza,
                Edad = perro.Edad
            };

            return (true, "Perro creado correctamente.", response);
        }

        public async Task<(bool Success, string Message, PerroResponseDto? Data)> ActualizarAsync(int perroId, int usuarioId, PerroUpdateRequestDto dto)
        {
            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == perroId && p.DueñoId == usuarioId);

            if (perro == null)
            {
                return (false, "Perro no encontrado.", null);
            }

            perro.Nombre = dto.Nombre;
            perro.Raza = dto.Raza;
            perro.Edad = dto.Edad;

            await _context.SaveChangesAsync();

            var response = new PerroResponseDto
            {
                Id = perro.Id,
                Nombre = perro.Nombre,
                Raza = perro.Raza,
                Edad = perro.Edad
            };

            return (true, "Perro actualizado correctamente.", response);
        }

        public async Task<(bool Success, string Message)> EliminarAsync(int perroId, int usuarioId)
        {
            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == perroId && p.DueñoId == usuarioId);

            if (perro == null)
            {
                return (false, "Perro no encontrado.");
            }

            _context.Perros.Remove(perro);
            await _context.SaveChangesAsync();

            return (true, "Perro eliminado correctamente.");
        }
    }
}