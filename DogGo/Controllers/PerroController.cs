using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

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
            var perros = await _context.Perros
                .Include(p => p.Dueño)
                .ToListAsync();

            return View(perros);
        }

        // GET: /Perro/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var perro = await _context.Perros
                .Include(p => p.Dueño)
                .Include(p => p.Paseos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perro == null)
                return NotFound();

            return View(perro);
        }

        // GET: /Perro/Create
        public async Task<IActionResult> Create()
        {
            await CargarDueños();
            return View();
        }

        // POST: /Perro/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Perro perro, IFormFile? imagenArchivo)
        {
            ModelState.Remove("Dueño");
            ModelState.Remove("Paseos");

            if (!ModelState.IsValid)
            {
                await CargarDueños(perro.DueñoId);
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

            var perro = await _context.Perros.FindAsync(id);
            if (perro == null)
                return NotFound();

            await CargarDueños(perro.DueñoId);
            return View(perro);
        }

        // POST: /Perro/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Perro perro)
        {
            if (id != perro.Id)
                return NotFound();

            ModelState.Remove("Dueño");
            ModelState.Remove("Paseos");

            if (!ModelState.IsValid)
            {
                await CargarDueños(perro.DueñoId);
                return View(perro);
            }

            try
            {
                _context.Update(perro);
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

            var perro = await _context.Perros
                .Include(p => p.Dueño)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perro == null)
                return NotFound();

            return View(perro);
        }

        // POST: /Perro/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var perro = await _context.Perros.FindAsync(id);

            if (perro != null)
            {
                _context.Perros.Remove(perro);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PerroExiste(int id)
        {
            return _context.Perros.Any(e => e.Id == id);
        }

        private async Task CargarDueños(object? dueñoSeleccionado = null)
        {
            var dueños = await _context.Usuarios
                .Where(u => u.Rol == "Duenio")
                .Select(u => new
                {
                    u.Id,
                    NombreCompleto = u.Nombre + " " + u.Apellido
                })
                .ToListAsync();

            ViewBag.DueñoId = new SelectList(dueños, "Id", "NombreCompleto", dueñoSeleccionado);
        }
    }
}