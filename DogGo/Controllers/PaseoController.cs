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
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
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
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
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

            if (duracionMinutos <= 0)
            {
                TempData["Error"] = "La duración del paseo no es válida.";
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
                FechaInicio = esProgramado ? fechaProgramada!.Value.ToUniversalTime() : DateTime.UtcNow,
                Estado = "Pendiente",
                Precio = precio,
                DuracionMinutos = duracionMinutos,
                EsProgramado = esProgramado,
                FechaProgramada = esProgramado ? fechaProgramada!.Value.ToUniversalTime() : null
            };

            _context.Paseos.Add(paseo);
            await _context.SaveChangesAsync();

            TempData["Exito"] = esProgramado
                ? "Paseo programado correctamente."
                : "Paseo solicitado correctamente.";

            return RedirectToAction("Mapa", new { id = paseo.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string? motivo)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var paseo = await _context.Paseos
                .Include(p => p.Perro)
                .Include(p => p.Paseador)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paseo == null)
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
            paseo.CanceladoPor = esDuenio ? "Dueño" : "Paseador";
            paseo.FechaCancelacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Exito"] = "El paseo fue cancelado correctamente.";
            return RedirectToAction("MisPaseos");
        }
    }
}