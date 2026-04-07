using DogGo.Data;
using DogGo.Models;
using DogGo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DogGo.Controllers
{
    [Authorize]
    public class CalificacionController : Controller
    {
        private readonly DogGoDbContext _context;

        public CalificacionController(DogGoDbContext context)
        {
            _context = context;
        }

        // GET: /Calificacion/Crear/5
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Crear(int paseoId)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseo = await _context.Paseos
                .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                .Include(p => p.Perro)
                .Include(p => p.Calificacion)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
                return NotFound();

            if (paseo.Perro.DueñoId != usuarioId)
                return Forbid();

            if (paseo.Estado != "Finalizado")
            {
                TempData["Error"] = "Solo puedes calificar un paseo finalizado.";
                return RedirectToAction("Mapa", "Paseo", new { id = paseoId });
            }

            if (paseo.Calificacion != null)
            {
                TempData["Error"] = "Este paseo ya fue calificado.";
                return RedirectToAction("Mapa", "Paseo", new { id = paseoId });
            }

            ViewBag.Paseo = paseo;
            return View(new CalificacionViewModel { PaseoId = paseoId });
        }

        // POST: /Calificacion/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Crear(CalificacionViewModel model)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseo = await _context.Paseos
                .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                .Include(p => p.Perro)
                .Include(p => p.Calificacion)
                .FirstOrDefaultAsync(p => p.Id == model.PaseoId);

            if (paseo == null)
                return NotFound();

            if (paseo.Perro.DueñoId != usuarioId)
                return Forbid();

            if (paseo.Estado != "Finalizado")
            {
                TempData["Error"] = "Solo puedes calificar un paseo finalizado.";
                return RedirectToAction("Mapa", "Paseo", new { id = model.PaseoId });
            }

            if (paseo.Calificacion != null)
            {
                TempData["Error"] = "Este paseo ya fue calificado.";
                return RedirectToAction("Mapa", "Paseo", new { id = model.PaseoId });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Paseo = paseo;
                return View(model);
            }

            var calificacion = new Calificacion
            {
                PaseoId = model.PaseoId,
                DueñoId = usuarioId,
                Puntaje = model.Puntaje,
                Comentario = model.Comentario,
                Fecha = DateTime.UtcNow
            };

            _context.Calificaciones.Add(calificacion);
            await _context.SaveChangesAsync();

            await RecalcularPromedio(model.PaseoId);

            TempData["Exito"] = "¡Calificación enviada con éxito!";
            return RedirectToAction("MisPaseos", "Paseo");
        }

        // Helper: recalcular promedio
        private async Task RecalcularPromedio(int paseoId)
        {
            var paseo = await _context.Paseos
                .Include(p => p.Paseador)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null)
                return;

            var promedio = await _context.Calificaciones
                .Where(c => c.Paseo.PaseadorId == paseo.PaseadorId)
                .AverageAsync(c => (double)c.Puntaje);

            paseo.Paseador.CalificacionPromedio = (decimal)Math.Round(promedio, 2);
            await _context.SaveChangesAsync();
        }
    }
}