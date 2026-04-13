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
        private readonly IWebHostEnvironment _environment;

        public PaseoController(DogGoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Paseo/Mapa/5
        public async Task<IActionResult> Mapa(int id)
        {
            var redireccionPerfil = await RedirigirSiPerfilPaseadorIncompleto();
            if (redireccionPerfil != null)
                return redireccionPerfil;

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
            var redireccionPerfil = await RedirigirSiPerfilPaseadorIncompleto();
            if (redireccionPerfil != null)
                return redireccionPerfil;

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var rol = User.FindFirstValue(ClaimTypes.Role);

            List<Paseo> paseos;

            if (rol == "Paseador")
            {
                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null)
                    return View(new List<Paseo>());

                paseos = await _context.Paseos
                    .Where(p => p.PaseadorId == paseador.Id)
                    .Include(p => p.Perro).ThenInclude(pe => pe.Dueño)
                    .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                    .Include(p => p.Calificacion)
                    .OrderByDescending(p => p.EsProgramado && p.FechaProgramada != null
                        ? p.FechaProgramada
                        : p.FechaInicio)
                    .ToListAsync();
            }
            else
            {
                paseos = await _context.Paseos
                    .Where(p => p.Perro.DueñoId == usuarioId)
                    .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                    .Include(p => p.Perro)
                    .Include(p => p.Calificacion)
                    .OrderByDescending(p => p.EsProgramado && p.FechaProgramada != null
                        ? p.FechaProgramada
                        : p.FechaInicio)
                    .ToListAsync();
            }

            return View(paseos);
        }

        // POST: /Paseo/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Crear(
            int paseadorId,
            int perroId,
            decimal precio,
            int duracionMinutos,
            bool esProgramado,
            DateTime? fechaProgramada)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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

            if (duracionMinutos < 15 || duracionMinutos > 180)
            {
                TempData["Error"] = "La duración del paseo debe estar entre 15 y 180 minutos.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (string.IsNullOrWhiteSpace(paseador.Descripcion) ||
                string.IsNullOrWhiteSpace(paseador.FotoUrl) ||
                string.IsNullOrWhiteSpace(paseador.ZonaServicio) ||
                !paseador.ExperienciaAnios.HasValue ||
                paseador.ExperienciaAnios.Value < 0 ||
                paseador.TarifaPorHora <= 0)
            {
                TempData["Error"] = "El paseador aún no tiene su perfil completo.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (!paseador.Disponible)
            {
                TempData["Error"] = "El paseador no está disponible en este momento.";
                return RedirectToAction("Directorio", "Paseador");
            }

            var paseoExistentePerro = await _context.Paseos
                .AnyAsync(p => p.PerroId == perroId &&
                              (p.Estado == "Pendiente" || p.Estado == "EnCurso"));

            if (paseoExistentePerro)
            {
                TempData["Error"] = "Este perro ya tiene un paseo pendiente o en curso.";
                return RedirectToAction("Index", "Perro");
            }

            var paseadorEnCurso = await _context.Paseos
                .AnyAsync(p => p.PaseadorId == paseadorId && p.Estado == "EnCurso");

            if (paseadorEnCurso)
            {
                TempData["Error"] = "El paseador ya tiene un paseo en curso.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (esProgramado)
            {
                if (fechaProgramada == null)
                {
                    TempData["Error"] = "Debes seleccionar una fecha y hora para programar el paseo.";
                    return RedirectToAction("Directorio", "Paseador");
                }

                if (fechaProgramada <= DateTime.Now)
                {
                    TempData["Error"] = "La fecha programada debe ser futura.";
                    return RedirectToAction("Directorio", "Paseador");
                }

                var nuevaInicioUtc = fechaProgramada.Value.ToUniversalTime();
                var nuevaFinUtc = nuevaInicioUtc.AddMinutes(duracionMinutos);

                var paseosProgramados = await _context.Paseos
                    .Where(p =>
                        p.PaseadorId == paseadorId &&
                        p.Estado == "Pendiente" &&
                        p.EsProgramado &&
                        p.FechaProgramada != null)
                    .ToListAsync();

                var paseoEmpalmado = paseosProgramados.Any(p =>
                {
                    var existenteInicioUtc = p.FechaProgramada!.Value;
                    var existenteFinUtc = existenteInicioUtc.AddMinutes(p.DuracionMinutos);

                    return nuevaInicioUtc < existenteFinUtc &&
                           nuevaFinUtc > existenteInicioUtc;
                });

                if (paseoEmpalmado)
                {
                    TempData["Error"] = "El paseador ya tiene otro paseo programado que se empalma con ese horario.";
                    return RedirectToAction("Directorio", "Paseador");
                }
            }

            var paseo = new Paseo
            {
                PaseadorId = paseadorId,
                PerroId = perroId,
                FechaInicio = null,
                FechaFin = null,
                Estado = "Pendiente",
                Precio = precio,
                DuracionMinutos = duracionMinutos,
                EsProgramado = esProgramado,
                FechaProgramada = esProgramado ? fechaProgramada!.Value.ToUniversalTime() : null,
                FotoInicioUrl = null,
                FotoFinUrl = null
            };

            _context.Paseos.Add(paseo);
            await _context.SaveChangesAsync();

            TempData["Exito"] = esProgramado
                ? "Paseo programado correctamente."
                : "Paseo solicitado correctamente.";

            return RedirectToAction("Mapa", new { id = paseo.Id });
        }

        // POST: /Paseo/SubirFotoInicio
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paseador")]
        public async Task<IActionResult> SubirFotoInicio(int paseoId, IFormFile? fotoInicio)
        {
            var redireccionPerfil = await RedirigirSiPerfilPaseadorIncompleto();
            if (redireccionPerfil != null)
                return redireccionPerfil;

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseo = await _context.Paseos
                .Include(p => p.Paseador)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null || paseo.Paseador == null)
                return NotFound();

            if (paseo.Paseador.UsuarioId != usuarioId)
                return Forbid();

            if (paseo.Estado != "Pendiente")
            {
                TempData["Error"] = "Solo puedes subir la foto antes de iniciar un paseo pendiente.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            if (fotoInicio == null || fotoInicio.Length == 0)
            {
                TempData["Error"] = "Debes subir una foto del perro para iniciar el paseo.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(fotoInicio.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
            {
                TempData["Error"] = "Solo se permiten imágenes JPG, JPEG, PNG o WEBP.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            if (fotoInicio.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "La imagen no debe superar los 5 MB.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "paseos");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = Guid.NewGuid().ToString() + extension;
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await fotoInicio.CopyToAsync(stream);
            }

            paseo.FotoInicioUrl = "/uploads/paseos/" + nombreArchivo;

            await _context.SaveChangesAsync();

            TempData["Exito"] = "Foto de inicio subida correctamente.";
            return RedirectToAction("Mapa", new { id = paseoId });
        }

        // POST: /Paseo/SubirFotoFin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paseador")]
        public async Task<IActionResult> SubirFotoFin(int paseoId, IFormFile? fotoFin)
        {
            var redireccionPerfil = await RedirigirSiPerfilPaseadorIncompleto();
            if (redireccionPerfil != null)
                return redireccionPerfil;

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseo = await _context.Paseos
                .Include(p => p.Paseador)
                .FirstOrDefaultAsync(p => p.Id == paseoId);

            if (paseo == null || paseo.Paseador == null)
                return NotFound();

            if (paseo.Paseador.UsuarioId != usuarioId)
                return Forbid();

            if (paseo.Estado != "EnCurso")
            {
                TempData["Error"] = "Solo puedes subir la foto final durante un paseo en curso.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            if (fotoFin == null || fotoFin.Length == 0)
            {
                TempData["Error"] = "Debes subir una foto final antes de terminar el paseo.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(fotoFin.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
            {
                TempData["Error"] = "Solo se permiten imágenes JPG, JPEG, PNG o WEBP.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            if (fotoFin.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "La imagen no debe superar los 5 MB.";
                return RedirectToAction("Mapa", new { id = paseoId });
            }

            var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "paseos");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = Guid.NewGuid().ToString() + extension;
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await fotoFin.CopyToAsync(stream);
            }

            paseo.FotoFinUrl = "/uploads/paseos/" + nombreArchivo;

            await _context.SaveChangesAsync();

            TempData["Exito"] = "Foto final subida correctamente.";
            return RedirectToAction("Mapa", new { id = paseoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string? motivo)
        {
            var redireccionPerfil = await RedirigirSiPerfilPaseadorIncompleto();
            if (redireccionPerfil != null)
                return redireccionPerfil;

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseo = await _context.Paseos
                .Include(p => p.Perro)
                .Include(p => p.Paseador)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paseo == null)
                return NotFound();

            if (paseo.Perro == null || paseo.Paseador == null)
                return NotFound();

            var esDuenio = paseo.Perro.DueñoId == usuarioId;
            var esPaseador = paseo.Paseador.UsuarioId == usuarioId;

            if (!esDuenio && !esPaseador)
                return Forbid();

            if (paseo.Estado != "Pendiente")
            {
                TempData["Error"] = "Solo se pueden cancelar paseos pendientes.";
                return RedirectToAction("MisPaseos");
            }

            paseo.Estado = "Cancelado";
            paseo.MotivoCancelacion = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
            paseo.CanceladoPor = esDuenio ? "Duenio" : "Paseador";
            paseo.FechaCancelacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Exito"] = "El paseo fue cancelado correctamente.";
            return RedirectToAction("MisPaseos");
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