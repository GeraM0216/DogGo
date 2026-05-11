using System.Security.Claims;
using DogGo.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/paseadores")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PaseadoresResenasApiController : ControllerBase
    {
        private readonly DogGoDbContext _context;

        public PaseadoresResenasApiController(DogGoDbContext context)
        {
            _context = context;
        }

        [HttpGet("{paseadorId:int}/resenas")]
        [HttpGet("{paseadorId:int}/reseñas")]
        [HttpGet("{paseadorId:int}/calificaciones")]
        public async Task<IActionResult> ObtenerResenasDePaseador(int paseadorId)
        {
            var existe = await _context.Paseadores.AnyAsync(p => p.Id == paseadorId);

            if (!existe)
            {
                return NotFound(new { success = false, message = "Paseador no encontrado." });
            }

            var data = await ConstruirRespuestaResenasAsync(paseadorId);

            return Ok(new
            {
                success = true,
                message = "Reseñas obtenidas correctamente.",
                data
            });
        }

        [HttpGet("mi-perfil/resenas")]
        [HttpGet("mi-perfil/reseñas")]
        [HttpGet("mi-perfil/calificaciones")]
        public async Task<IActionResult> ObtenerMisResenasComoPaseador()
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId.Value);

            if (paseador == null)
            {
                return NotFound(new { success = false, message = "No existe perfil de paseador para este usuario." });
            }

            var data = await ConstruirRespuestaResenasAsync(paseador.Id);

            return Ok(new
            {
                success = true,
                message = "Reseñas obtenidas correctamente.",
                data
            });
        }

        private async Task<object> ConstruirRespuestaResenasAsync(int paseadorId)
        {
            var resenas = await _context.Calificaciones
                .Include(c => c.Dueño)
                .Include(c => c.Paseo)
                    .ThenInclude(p => p.Perro)
                .Where(c => c.Paseo.PaseadorId == paseadorId)
                .OrderByDescending(c => c.Fecha)
                .Select(c => new
                {
                    id = c.Id,
                    paseoId = c.PaseoId,
                    puntaje = c.Puntaje,
                    comentario = c.Comentario,
                    fecha = c.Fecha,
                    duenioId = c.DueñoId,
                    duenioNombre = c.Dueño.Nombre,
                    duenioApellido = c.Dueño.Apellido,
                    duenioNombreCompleto = (c.Dueño.Nombre + " " + c.Dueño.Apellido).Trim(),
                    perroId = c.Paseo.PerroId,
                    perroNombre = c.Paseo.Perro.Nombre
                })
                .ToListAsync();

            var total = resenas.Count;
            var promedio = total == 0 ? 0 : Math.Round(resenas.Average(r => r.puntaje), 2);

            return new
            {
                paseadorId,
                total,
                promedio,
                calificacionPromedio = promedio,
                resenas
            };
        }

        private int? ObtenerUsuarioId()
        {
            var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("usuarioId")
                ?? User.FindFirstValue("UsuarioId");

            return int.TryParse(valor, out var usuarioId) ? usuarioId : null;
        }
    }
}