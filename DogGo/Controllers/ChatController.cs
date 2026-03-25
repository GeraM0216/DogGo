using DogGo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DogGo.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly DogGoDbContext _context;

        public ChatController(DogGoDbContext context)
        {
            _context = context;
        }

        // GET: /Chat/Index/5  (5 = paseoId)
        public async Task<IActionResult> Index(int paseoId)
        {
            var usuarioId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            // Verificar que el usuario tenga acceso a este paseo
            var paseo = await _context.Paseos
                .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                .Include(p => p.Perro).ThenInclude(pe => pe.Dueño)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null) return NotFound();

            var esDuenio = paseo.Perro.DueñoId == usuarioId;
            var esPaseador = paseo.Paseador.UsuarioId == usuarioId;

            if (!esDuenio && !esPaseador) return Forbid();

            // Determinar quién es el receptor
            var receptorId = esDuenio
                ? paseo.Paseador.UsuarioId
                : paseo.Perro.DueñoId;

            // Cargar historial de mensajes
            var mensajes = await _context.Mensajes
                .Where(m => m.PaseoId == paseoId)
                .Include(m => m.Emisor)
                .OrderBy(m => m.FechaEnvio)
                .ToListAsync();

            // Marcar mensajes recibidos como leídos
            var noLeidos = mensajes
                .Where(m => m.ReceptorId == usuarioId && !m.Leido)
                .ToList();

            noLeidos.ForEach(m => m.Leido = true);
            await _context.SaveChangesAsync();

            ViewBag.PaseoId = paseoId;
            ViewBag.UsuarioId = usuarioId;
            ViewBag.ReceptorId = receptorId;
            ViewBag.Mensajes = mensajes;

            return View();
        }
    }
}
