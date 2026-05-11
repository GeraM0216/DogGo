using System.Security.Claims;
using DogGo.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotificacionesApiController : ControllerBase
    {
        private readonly DogGoDbContext _context;

        public NotificacionesApiController(DogGoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet("mis-notificaciones")]
        public async Task<IActionResult> ObtenerMisNotificaciones()
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId.Value)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(80)
                .Select(n => new
                {
                    id = n.Id,
                    titulo = n.Titulo,
                    mensaje = n.Mensaje,
                    tipo = n.Tipo,
                    referenciaId = n.ReferenciaId,
                    leida = n.Leida,
                    fechaCreacion = n.FechaCreacion,
                    fecha = n.FechaCreacion
                })
                .ToListAsync();

            var noLeidas = await _context.Notificaciones
                .CountAsync(n => n.UsuarioId == usuarioId.Value && !n.Leida);

            return Ok(new
            {
                success = true,
                message = "Notificaciones obtenidas correctamente.",
                data = notificaciones,
                noLeidas
            });
        }

        [HttpPut("{notificacionId:int}/leida")]
        [HttpPut("marcar-leida/{notificacionId:int}")]
        public async Task<IActionResult> MarcarComoLeida(int notificacionId)
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == notificacionId && n.UsuarioId == usuarioId.Value);

            if (notificacion == null)
            {
                return NotFound(new { success = false, message = "Notificación no encontrada." });
            }

            notificacion.Leida = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Notificación marcada como leída." });
        }

        [HttpPut("leidas")]
        [HttpPut("marcar-todas-leidas")]
        public async Task<IActionResult> MarcarTodasComoLeidas()
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId.Value && !n.Leida)
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.Leida = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Notificaciones marcadas como leídas." });
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