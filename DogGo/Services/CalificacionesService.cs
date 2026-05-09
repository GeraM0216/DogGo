using DogGo.Data;
using DogGo.DTOs.Calificaciones;
using DogGo.Models;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Services
{
    public class CalificacionService
    {
        private readonly DogGoDbContext _context;

        public CalificacionService(DogGoDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, CalificacionResponseDto? Data)> CrearAsync(
            int paseoId,
            int usuarioId,
            string rol,
            CrearCalificacionRequestDto dto)
        {
            if (dto == null)
            {
                return (false, "Datos inválidos.", null);
            }

            var validacion = ValidarCalificacion(dto);
            if (!validacion.Success)
            {
                return (false, validacion.Message, null);
            }

            var paseo = await QueryPaseoCompleto()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", null);
            }

            if (!EsDuenioDelPaseo(paseo, usuarioId) && !EsAdmin(rol))
            {
                return (false, "Solo el dueño del perro puede calificar este paseo.", null);
            }

            if (paseo.Estado != "Finalizado")
            {
                return (false, "Solo se pueden calificar paseos finalizados.", null);
            }

            var yaExiste = await _context.Calificaciones
                .AnyAsync(c => c.PaseoId == paseoId);

            if (yaExiste)
            {
                return (false, "Este paseo ya fue calificado.", null);
            }

            var duenioId = EsAdmin(rol) && paseo.Perro?.DueñoId > 0
                ? paseo.Perro.DueñoId
                : usuarioId;

            var calificacion = new Calificacion
            {
                PaseoId = paseoId,
                DueñoId = duenioId,
                Puntaje = dto.Puntaje,
                Comentario = TextoONull(dto.Comentario),
                Fecha = DateTime.UtcNow
            };

            _context.Calificaciones.Add(calificacion);
            await _context.SaveChangesAsync();

            await ActualizarPromedioPaseadorAsync(paseo.PaseadorId);

            var guardada = await QueryCalificacionesCompletas()
                .FirstAsync(c => c.Id == calificacion.Id);

            return (true, "Calificación registrada correctamente.", MapResponse(guardada));
        }

        public async Task<(bool Success, string Message, CalificacionResponseDto? Data)> ObtenerPorPaseoAsync(
            int paseoId,
            int usuarioId,
            string rol)
        {
            var paseo = await QueryPaseoCompleto()
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
            {
                return (false, "Paseo no encontrado.", null);
            }

            if (!UsuarioPuedeVerPaseo(paseo, usuarioId, rol))
            {
                return (false, "No tienes permiso para ver esta calificación.", null);
            }

            var calificacion = await QueryCalificacionesCompletas()
                .FirstOrDefaultAsync(c => c.PaseoId == paseoId);

            if (calificacion == null)
            {
                return (false, "Este paseo todavía no tiene calificación.", null);
            }

            return (true, "Calificación obtenida correctamente.", MapResponse(calificacion));
        }

        public async Task<(bool Success, string Message, List<CalificacionResponseDto> Data)> ObtenerPorPaseadorAsync(
            int paseadorId)
        {
            var existePaseador = await _context.Paseadores.AnyAsync(p => p.Id == paseadorId);

            if (!existePaseador)
            {
                return (false, "Paseador no encontrado.", new List<CalificacionResponseDto>());
            }

            var calificaciones = await QueryCalificacionesCompletas()
                .Where(c => c.Paseo.PaseadorId == paseadorId)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            return (true, "Calificaciones obtenidas correctamente.", calificaciones.Select(MapResponse).ToList());
        }

        private IQueryable<Paseo> QueryPaseoCompleto()
        {
            return _context.Paseos
                .Include(p => p.Perro)
                    .ThenInclude(perro => perro.Dueño)
                .Include(p => p.Paseador)
                    .ThenInclude(paseador => paseador.Usuario);
        }

        private IQueryable<Calificacion> QueryCalificacionesCompletas()
        {
            return _context.Calificaciones
                .Include(c => c.Dueño)
                .Include(c => c.Paseo)
                    .ThenInclude(p => p.Perro)
                        .ThenInclude(perro => perro.Dueño)
                .Include(c => c.Paseo)
                    .ThenInclude(p => p.Paseador)
                        .ThenInclude(paseador => paseador.Usuario);
        }

        private async Task ActualizarPromedioPaseadorAsync(int paseadorId)
        {
            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.Id == paseadorId);

            if (paseador == null)
            {
                return;
            }

            var promedio = await _context.Calificaciones
                .Where(c => c.Paseo.PaseadorId == paseadorId)
                .AverageAsync(c => (decimal?)c.Puntaje);

            paseador.CalificacionPromedio = promedio == null
                ? 0
                : Math.Round(promedio.Value, 2);

            await _context.SaveChangesAsync();
        }

        private static (bool Success, string Message) ValidarCalificacion(CrearCalificacionRequestDto dto)
        {
            if (dto.Puntaje < 1 || dto.Puntaje > 5)
            {
                return (false, "El puntaje debe estar entre 1 y 5.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Comentario) && dto.Comentario.Trim().Length > 500)
            {
                return (false, "El comentario no puede superar 500 caracteres.");
            }

            return (true, "OK");
        }

        private static bool EsDuenioDelPaseo(Paseo paseo, int usuarioId)
        {
            return paseo.Perro.DueñoId == usuarioId;
        }

        private static bool EsAdmin(string? rol)
        {
            return string.Equals(rol?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private static bool UsuarioPuedeVerPaseo(Paseo paseo, int usuarioId, string? rol)
        {
            if (EsAdmin(rol))
            {
                return true;
            }

            if (paseo.Perro.DueñoId == usuarioId)
            {
                return true;
            }

            if (paseo.Paseador.UsuarioId == usuarioId)
            {
                return true;
            }

            return false;
        }

        private static string? TextoONull(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            return texto.Trim();
        }

        private static CalificacionResponseDto MapResponse(Calificacion calificacion)
        {
            var duenio = calificacion.Dueño;
            var paseo = calificacion.Paseo;
            var usuarioPaseador = paseo.Paseador.Usuario;

            var duenioNombreCompleto = $"{duenio.Nombre} {duenio.Apellido}".Trim();
            var paseadorNombreCompleto = $"{usuarioPaseador.Nombre} {usuarioPaseador.Apellido}".Trim();

            return new CalificacionResponseDto
            {
                Id = calificacion.Id,
                PaseoId = calificacion.PaseoId,

                DuenioId = duenio.Id,
                DuenioNombre = duenio.Nombre,
                DuenioApellido = duenio.Apellido,
                DuenioNombreCompleto = duenioNombreCompleto,

                PaseadorId = paseo.PaseadorId,
                PaseadorNombre = usuarioPaseador.Nombre,
                PaseadorApellido = usuarioPaseador.Apellido,
                PaseadorNombreCompleto = paseadorNombreCompleto,

                Puntaje = calificacion.Puntaje,
                Comentario = calificacion.Comentario,
                Fecha = calificacion.Fecha,

                CalificacionPromedioPaseador = paseo.Paseador.CalificacionPromedio
            };
        }
    }
}
