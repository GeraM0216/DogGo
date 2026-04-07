using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DogGo.Controllers
{
    [Authorize]
    public class PaseoController : Controller
    {
        private readonly DogGoDbContext _context;

        public PaseoController(DogGoDbContext context)
        {
            _context = context;
        }

        // GET: /Paseo/Mapa/5
        public async Task<IActionResult> Mapa(int id)
        {
            var usuarioId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            var paseo = await _context.Paseos
                .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                .Include(p => p.Perro).ThenInclude(pe => pe.Dueño)
                .Include(p => p.Ubicaciones.OrderBy(u => u.Timestamp))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paseo == null || paseo.Perro == null || paseo.Paseador == null)
                return NotFound();

            var esDuenio = paseo.Perro.DueñoId == usuarioId;
            var esPaseador = paseo.Paseador.UsuarioId == usuarioId;

            if (!esDuenio && !esPaseador)
                return Forbid();

            ViewBag.Paseo = paseo;
            ViewBag.EsPaseador = esPaseador;
            ViewBag.UsuarioId = usuarioId;

            return View(paseo);
        }

        // GET: /Paseo/MisPaseos
        public async Task<IActionResult> MisPaseos()
        {
            var usuarioId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );
            var rol = User.FindFirstValue(ClaimTypes.Role);

            List<Paseo> paseos;

            if (rol == "Paseador")
            {
                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null)
                {
                    return View(new List<Paseo>());
                }

                paseos = await _context.Paseos
                    .Where(p => p.PaseadorId == paseador.Id)
                    .Include(p => p.Perro).ThenInclude(pe => pe.Dueño)
                    .Include(p => p.Calificacion)
                    .OrderByDescending(p => p.FechaInicio)
                    .ToListAsync();
            }
            else
            {
                paseos = await _context.Paseos
                    .Where(p => p.Perro.DueñoId == usuarioId)
                    .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                    .Include(p => p.Perro)
                    .Include(p => p.Calificacion)
                    .OrderByDescending(p => p.FechaInicio)
                    .ToListAsync();
            }

            return View(paseos);
        }

        // POST: /Paseo/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Crear(int paseadorId, int perroId, decimal precio)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == perroId);

            if (perro == null)
                return NotFound();

            if (perro.DueñoId != usuarioId)
                return Forbid();

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.Id == paseadorId);

            if (paseador == null)
                return NotFound();

            if (precio <= 0)
            {
                TempData["Error"] = "El precio del paseo no es válido.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (!paseador.Disponible)
            {
                TempData["Error"] = "El paseador no está disponible en este momento.";
                return RedirectToAction("Directorio", "Paseador");
            }

            var paseoExistente = await _context.Paseos
                .AnyAsync(p => p.PerroId == perroId &&
                              (p.Estado == "Pendiente" || p.Estado == "EnCurso"));

            if (paseoExistente)
            {
                TempData["Error"] = "Este perro ya tiene un paseo pendiente o en curso.";
                return RedirectToAction("Index", "Perro");
            }

            var paseo = new Paseo
            {
                PaseadorId = paseadorId,
                PerroId = perroId,
                FechaInicio = DateTime.UtcNow,
                Estado = "Pendiente",
                Precio = precio
            };

            _context.Paseos.Add(paseo);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Paseo solicitado correctamente.";
            return RedirectToAction("Mapa", new { id = paseo.Id });
        }
    }
}