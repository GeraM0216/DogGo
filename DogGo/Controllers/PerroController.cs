using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DogGo.Controllers
{
    [Authorize]
    public class PerroController : Controller
    {
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PerroController(DogGoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Perro
        public async Task<IActionResult> Index()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            var perros = await _context.Perros
                .Include(p => p.Dueño)
                .Where(p => p.DueñoId == usuarioId)
                .ToListAsync();

            return View(perros);
        }

        // GET: /Perro/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            var perro = await _context.Perros
                .Include(p => p.Dueño)
                .Include(p => p.Paseos)
                .FirstOrDefaultAsync(p => p.Id == id && p.DueñoId == usuarioId);

            if (perro == null)
                return NotFound();

            return View(perro);
        }

        // GET: /Perro/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Perro/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Perro perro, IFormFile? imagenArchivo)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            perro.DueñoId = usuarioId;

            ModelState.Remove("Dueño");
            ModelState.Remove("Paseos");
            ModelState.Remove("DueñoId");

            if (!ModelState.IsValid)
            {
                return View(perro);
            }

            if (imagenArchivo != null && imagenArchivo.Length > 0)
            {
                var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "perros");
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenArchivo.FileName);
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await imagenArchivo.CopyToAsync(stream);
                }

                perro.ImagenUrl = "/uploads/perros/" + nombreArchivo;
            }

            _context.Perros.Add(perro);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Perro/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == id && p.DueñoId == usuarioId);

            if (perro == null)
                return NotFound();

            return View(perro);
        }

        // POST: /Perro/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Perro perro)
        {
            if (id != perro.Id)
                return NotFound();

            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            var perroExistente = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == id && p.DueñoId == usuarioId);

            if (perroExistente == null)
                return NotFound();

            ModelState.Remove("Dueño");
            ModelState.Remove("Paseos");
            ModelState.Remove("DueñoId");

            if (!ModelState.IsValid)
            {
                perro.DueñoId = usuarioId;
                return View(perro);
            }

            try
            {
                perroExistente.Nombre = perro.Nombre;
                perroExistente.Raza = perro.Raza;
                perroExistente.Edad = perro.Edad;
                perroExistente.Tamaño = perro.Tamaño;
                perroExistente.Notas = perro.Notas;
                perroExistente.ImagenUrl = perro.ImagenUrl;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PerroExiste(perro.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Perro/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            var perro = await _context.Perros
                .Include(p => p.Dueño)
                .FirstOrDefaultAsync(p => p.Id == id && p.DueñoId == usuarioId);

            if (perro == null)
                return NotFound();

            return View(perro);
        }

        // POST: /Perro/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            var perro = await _context.Perros
                .FirstOrDefaultAsync(p => p.Id == id && p.DueñoId == usuarioId);

            if (perro != null)
            {
                _context.Perros.Remove(perro);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: /Perro/MisPerrosJson
        // Devuelve las mascotas del usuario logueado como JSON
        // Lo usa el widget de la página de inicio.
        [HttpGet]
        public async Task<IActionResult> MisPerrosJson()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return Unauthorized();

            var usuarioId = int.Parse(usuarioIdClaim);

            var perros = await _context.Perros
                .Where(p => p.DueñoId == usuarioId)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Raza,
                    p.Edad,
                    p.Tamaño,
                    p.Notas,
                    p.ImagenUrl
                })
                .ToListAsync();

            return Json(perros);
        }

        private bool PerroExiste(int id)
        {
            return _context.Perros.Any(e => e.Id == id);
        }



    }
}