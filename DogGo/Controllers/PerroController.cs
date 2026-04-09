using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            ValidarImagen(imagenArchivo);

            if (!ModelState.IsValid)
                return View(perro);

            if (imagenArchivo != null && imagenArchivo.Length > 0)
            {
                try
                {
                    perro.ImagenUrl = await GuardarImagenPerro(imagenArchivo);
                }
                catch
                {
                    ModelState.AddModelError("ImagenUrl", "Ocurrió un error al guardar la imagen.");
                    return View(perro);
                }
            }

            _context.Perros.Add(perro);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Perro agregado correctamente.";
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
        public async Task<IActionResult> Edit(int id, Perro perro, IFormFile? imagenArchivo)
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

            ValidarImagen(imagenArchivo);

            if (!ModelState.IsValid)
            {
                perro.DueñoId = usuarioId;
                perro.ImagenUrl = perroExistente.ImagenUrl;
                return View(perro);
            }

            try
            {
                perroExistente.Nombre = perro.Nombre;
                perroExistente.Raza = perro.Raza;
                perroExistente.Edad = perro.Edad;
                perroExistente.Tamaño = perro.Tamaño;
                perroExistente.Notas = perro.Notas;

                if (imagenArchivo != null && imagenArchivo.Length > 0)
                {
                    var imagenAnterior = perroExistente.ImagenUrl;

                    perroExistente.ImagenUrl = await GuardarImagenPerro(imagenArchivo);

                    EliminarImagenSiExiste(imagenAnterior);
                }

                await _context.SaveChangesAsync();

                TempData["Exito"] = "Perro actualizado correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PerroExiste(perro.Id))
                    return NotFound();

                throw;
            }
            catch
            {
                ModelState.AddModelError("ImagenUrl", "Ocurrió un error al actualizar el perro o su imagen.");
                perro.ImagenUrl = perroExistente.ImagenUrl;
                return View(perro);
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
                var imagenUrl = perro.ImagenUrl;

                _context.Perros.Remove(perro);
                await _context.SaveChangesAsync();

                EliminarImagenSiExiste(imagenUrl);

                TempData["Exito"] = "Perro eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Perro/MisPerrosJson
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

        private void ValidarImagen(IFormFile? imagenArchivo)
        {
            if (imagenArchivo == null || imagenArchivo.Length == 0)
                return;

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imagenArchivo.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
            {
                ModelState.AddModelError("ImagenUrl", "Solo se permiten imágenes .jpg, .jpeg, .png o .webp.");
            }

            if (imagenArchivo.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImagenUrl", "La imagen no debe superar los 5 MB.");
            }
        }

        private async Task<string> GuardarImagenPerro(IFormFile imagenArchivo)
        {
            var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "perros");
            Directory.CreateDirectory(carpeta);

            var extension = Path.GetExtension(imagenArchivo.FileName).ToLowerInvariant();
            var nombreArchivo = Guid.NewGuid().ToString() + extension;
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await imagenArchivo.CopyToAsync(stream);
            }

            return "/uploads/perros/" + nombreArchivo;
        }

        private void EliminarImagenSiExiste(string? imagenUrl)
        {
            if (string.IsNullOrWhiteSpace(imagenUrl))
                return;

            var rutaRelativa = imagenUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var rutaFisica = Path.Combine(_environment.WebRootPath, rutaRelativa.Replace("uploads" + Path.DirectorySeparatorChar, ""));

            var rutaCompleta = Path.Combine(_environment.WebRootPath, rutaRelativa);

            if (System.IO.File.Exists(rutaCompleta))
            {
                System.IO.File.Delete(rutaCompleta);
            }
        }

        private bool PerroExiste(int id)
        {
            return _context.Perros.Any(e => e.Id == id);
        }
    }
}