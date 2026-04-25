using DogGo.Data;
using DogGo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DogGo.Controllers
{
    [Authorize]
    public class PaseadorController : Controller
    {
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PaseadorController(DogGoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Paseador/Perfil/5
        public async Task<IActionResult> Perfil(int? id)
        {
            if (!id.HasValue)
            {
                var redireccionPerfil = await RedirigirSiPerfilPaseadorIncompleto();
                if (redireccionPerfil != null)
                    return redireccionPerfil;
            }

            int paseadorId;

            if (id.HasValue)
            {
                paseadorId = id.Value;
            }
            else
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null)
                    return Forbid();

                paseadorId = paseador.Id;
            }

            var perfil = await _context.Paseadores
                .Include(p => p.Usuario)
                .Include(p => p.Paseos)
                    .ThenInclude(pa => pa.Calificacion)
                .Include(p => p.Paseos)
                    .ThenInclude(pa => pa.Perro)
                .FirstOrDefaultAsync(p => p.Id == paseadorId);

            if (perfil == null)
                return NotFound();

            var calificaciones = await _context.Calificaciones
                .Where(c => c.Paseo.PaseadorId == paseadorId)
                .Include(c => c.Dueño)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            var vm = new PaseadorPerfilViewModel
            {
                Paseador = perfil,
                Calificaciones = calificaciones,
                Promedio = calificaciones.Any() ? calificaciones.Average(c => c.Puntaje) : 0,
                TotalPaseos = perfil.Paseos.Count(p => p.Estado == "Finalizado")
            };

            return View(vm);
        }

        // GET: /Paseador/Editar
        [Authorize(Roles = "Paseador")]
        public async Task<IActionResult> Editar()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador == null)
                return NotFound();

            return View(paseador);
        }

        // POST: /Paseador/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paseador")]
        public async Task<IActionResult> Editar(
            int id,
            string? descripcion,
            decimal tarifaPorHora,
            bool disponible,
            string? zonaServicio,
            int? experienciaAnios,
            IFormFile? fotoArchivo)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);

            if (paseador == null)
                return NotFound();

            descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
            zonaServicio = string.IsNullOrWhiteSpace(zonaServicio) ? null : zonaServicio.Trim();

            if (descripcion == null)
                ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");

            if (zonaServicio == null)
                ModelState.AddModelError("ZonaServicio", "Debes seleccionar al menos una zona.");

            if (tarifaPorHora <= 0)
                ModelState.AddModelError("TarifaPorHora", "La tarifa debe ser mayor a 0.");

            if (!experienciaAnios.HasValue || experienciaAnios.Value < 0)
                ModelState.AddModelError("ExperienciaAnios", "Debes indicar los años de experiencia.");

            var yaTieneFoto = !string.IsNullOrWhiteSpace(paseador.FotoUrl);
            var subioNuevaFoto = fotoArchivo != null && fotoArchivo.Length > 0;

            if (!yaTieneFoto && !subioNuevaFoto)
                ModelState.AddModelError("FotoUrl", "La foto de perfil es obligatoria.");

            if (subioNuevaFoto)
            {
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(fotoArchivo!.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                    ModelState.AddModelError("FotoUrl", "Solo se permiten imágenes JPG, JPEG, PNG o WEBP.");

                if (fotoArchivo.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError("FotoUrl", "La imagen no debe superar los 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                paseador.Descripcion = descripcion ?? paseador.Descripcion;
                paseador.TarifaPorHora = tarifaPorHora;
                paseador.Disponible = disponible;
                paseador.ZonaServicio = zonaServicio;
                paseador.ExperienciaAnios = experienciaAnios;

                return View(paseador);
            }

            paseador.Descripcion = descripcion!;
            paseador.TarifaPorHora = tarifaPorHora;
            paseador.Disponible = disponible;
            paseador.ZonaServicio = zonaServicio;
            paseador.ExperienciaAnios = experienciaAnios;

            if (subioNuevaFoto)
            {
                var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "paseadores");
                Directory.CreateDirectory(carpeta);

                var extension = Path.GetExtension(fotoArchivo!.FileName).ToLowerInvariant();
                var nombreArchivo = Guid.NewGuid().ToString() + extension;
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await fotoArchivo.CopyToAsync(stream);
                }

                paseador.FotoUrl = "/uploads/paseadores/" + nombreArchivo;
            }

            await _context.SaveChangesAsync();

            TempData["Exito"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Perfil");
        }

        // GET: /Paseador/Directorio
        public async Task<IActionResult> Directorio(
            string? busqueda,
            string? zona,
            int? experienciaMinima,
            decimal? tarifaMaxima,
            string? orden)
        {
            var query = _context.Paseadores
                .Where(p => p.Disponible)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                query = query.Where(p =>
                    p.Usuario.Nombre.Contains(busqueda) ||
                    p.Usuario.Apellido.Contains(busqueda) ||
                    p.Descripcion.Contains(busqueda));
            }

            if (!string.IsNullOrWhiteSpace(zona))
            {
                zona = zona.Trim();

                query = query.Where(p =>
                    p.ZonaServicio != null &&
                    p.ZonaServicio.Contains(zona));
            }

            if (experienciaMinima.HasValue)
            {
                query = query.Where(p =>
                    p.ExperienciaAnios.HasValue &&
                    p.ExperienciaAnios.Value >= experienciaMinima.Value);
            }

            if (tarifaMaxima.HasValue)
            {
                query = query.Where(p => p.TarifaPorHora <= tarifaMaxima.Value);
            }

            query = orden switch
            {
                "tarifa_asc" => query.OrderBy(p => p.TarifaPorHora),
                "tarifa_desc" => query.OrderByDescending(p => p.TarifaPorHora),
                "experiencia_desc" => query.OrderByDescending(p => p.ExperienciaAnios),
                "calificacion_desc" => query.OrderByDescending(p => p.CalificacionPromedio),
                _ => query.OrderByDescending(p => p.CalificacionPromedio)
            };

            var paseadores = await query.ToListAsync();

            ViewBag.Busqueda = busqueda;
            ViewBag.Zona = zona;
            ViewBag.ExperienciaMinima = experienciaMinima;
            ViewBag.TarifaMaxima = tarifaMaxima;
            ViewBag.Orden = orden ?? "calificacion_desc";

            if (User.IsInRole("Duenio"))
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var perros = await _context.Perros
                    .Where(p => p.DueñoId == usuarioId)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                var duenioPerfil = await _context.DuenioPerfiles
                    .FirstOrDefaultAsync(dp => dp.UsuarioId == usuarioId);

                ViewBag.Perros = perros;
                ViewBag.DuenioPerfil = duenioPerfil;
            }

            return View(paseadores);
        }

        private async Task<IActionResult?> RedirigirSiPerfilPaseadorIncompleto()
        {
            if (!User.IsInRole("Paseador"))
                return null;

            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuarioIdClaim))
                return null;

            var usuarioId = int.Parse(usuarioIdClaim);

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paseador == null)
                return null;

            var perfilIncompleto =
                string.IsNullOrWhiteSpace(paseador.Descripcion) ||
                string.IsNullOrWhiteSpace(paseador.FotoUrl) ||
                string.IsNullOrWhiteSpace(paseador.ZonaServicio) ||
                !paseador.ExperienciaAnios.HasValue ||
                paseador.ExperienciaAnios.Value < 0 ||
                paseador.TarifaPorHora <= 0;

            if (!perfilIncompleto)
                return null;

            TempData["Error"] = "Debes completar tu perfil antes de continuar.";
            return RedirectToAction("Editar", "Paseador");
        }
    }
}