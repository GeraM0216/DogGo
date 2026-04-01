using DogGo.Data;
using DogGo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DogGo.Controllers
{
    [Authorize]
    public class PaseadorController : Controller
    {
        private readonly DogGoDbContext _context;

        public PaseadorController(DogGoDbContext context)
        {
            _context = context;
        }

        // GET: /Paseador/Perfil/5  (5 = paseadorId, opcional)
        // Sin id muestra el perfil propio del paseador logueado
        public async Task<IActionResult> Perfil(int? id)
        {
            int paseadorId;

            if (id.HasValue)
            {
                paseadorId = id.Value;
            }
            else
            {
                var usuarioId = int.Parse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)
                );
                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null) return Forbid();
                paseadorId = paseador.Id;
            }

            var perfil = await _context.Paseadores
                .Include(p => p.Usuario)
                .Include(p => p.Paseos)
                    .ThenInclude(pa => pa.Calificacion)
                .Include(p => p.Paseos)
                    .ThenInclude(pa => pa.Perro)
                .FirstOrDefaultAsync(p => p.Id == paseadorId);

            if (perfil == null) return NotFound();

            var calificaciones = await _context.Calificaciones
                .Where(c => c.Paseo.PaseadorId == paseadorId)
                .Include(c => c.Dueño)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            var vm = new PaseadorPerfilViewModel
            {
                Paseador = perfil,
                Calificaciones = calificaciones,
                Promedio = calificaciones.Any()
                                    ? calificaciones.Average(c => c.Puntaje)
                                    : 0,
                TotalPaseos = perfil.Paseos.Count
            };

            return View(vm);
        }

        // GET: /Paseador/Editar
        [Authorize(Roles = "Paseador")]
        public async Task<IActionResult> Editar()
        {
            var usuarioId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador == null) return NotFound();
            return View(paseador);
        }

        // POST: /Paseador/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paseador")]
        public async Task<IActionResult> Editar(int id, string descripcion, decimal tarifaPorHora, bool disponible)
        {
            var paseador = await _context.Paseadores.FindAsync(id);
            if (paseador == null) return NotFound();

            paseador.Descripcion = descripcion;
            paseador.TarifaPorHora = tarifaPorHora;
            paseador.Disponible = disponible;

            await _context.SaveChangesAsync();

            TempData["Exito"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Perfil");
        }

        // GET: /Paseador/Directorio
        // Lista de paseadores disponibles para que los dueÃ±os contraten
        // GET: /Paseador/Directorio
        // Lista de paseadores disponibles para que los dueños contraten
        public async Task<IActionResult> Directorio()
        {
            var paseadores = await _context.Paseadores
                .Where(p => p.Disponible)
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.CalificacionPromedio)
                .ToListAsync();

            if (User.IsInRole("Duenio"))
            {
                var usuarioId = int.Parse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)
                );

                var perros = await _context.Perros
                    .Where(p => p.DueñoId == usuarioId)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                ViewBag.Perros = perros;
            }

            return View(paseadores);
        }
    }
}