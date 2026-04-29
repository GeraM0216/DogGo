using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;

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
                .Include(p => p.PaseoPerros)
                    .ThenInclude(pp => pp.Perro)
                .Include(p => p.Ubicaciones.OrderBy(u => u.Timestamp))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paseo == null || paseo.Perro == null || paseo.Paseador == null)
                return NotFound();

            var esDuenio = paseo.Perro.DueñoId == usuarioId ||
                           paseo.PaseoPerros.Any(pp => pp.Perro != null && pp.Perro.DueñoId == usuarioId);

            var esPaseador = paseo.Paseador.UsuarioId == usuarioId;

            if (!esDuenio && !esPaseador)
                return Forbid();

            ViewBag.Paseo = paseo;
            ViewBag.EsPaseador = esPaseador;
            ViewBag.UsuarioId = usuarioId;

            return View(paseo);
        }

        // GET: /Paseo/MisPaseos
        public async Task<IActionResult> MisPaseos(
            string? estado,
            string? busqueda,
            string? tipo,
            string? orden)
        {
            var redireccionPerfil = await RedirigirSiPerfilPaseadorIncompleto();
            if (redireccionPerfil != null)
                return redireccionPerfil;

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var rol = User.FindFirstValue(ClaimTypes.Role);

            IQueryable<Paseo> query = _context.Paseos
                .Include(p => p.Perro).ThenInclude(pe => pe.Dueño)
                .Include(p => p.Paseador).ThenInclude(pa => pa.Usuario)
                .Include(p => p.PaseoPerros).ThenInclude(pp => pp.Perro)
                .Include(p => p.Calificacion);

            if (rol == "Paseador")
            {
                var paseador = await _context.Paseadores
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                if (paseador == null)
                    return View(new List<Paseo>());

                query = query.Where(p => p.PaseadorId == paseador.Id);
            }
            else
            {
                query = query.Where(p =>
                    p.Perro.DueñoId == usuarioId ||
                    p.PaseoPerros.Any(pp => pp.Perro.DueñoId == usuarioId));
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                estado = estado.Trim();
                query = query.Where(p => p.Estado == estado);
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                query = query.Where(p =>
                    p.Perro.Nombre.Contains(busqueda) ||
                    p.PaseoPerros.Any(pp => pp.Perro.Nombre.Contains(busqueda)) ||
                    p.Paseador.Usuario.Nombre.Contains(busqueda) ||
                    p.Paseador.Usuario.Apellido.Contains(busqueda));
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                if (tipo == "Programado")
                    query = query.Where(p => p.EsProgramado);

                if (tipo == "Ahora")
                    query = query.Where(p => !p.EsProgramado);
            }

            query = orden switch
            {
                "fecha_asc" => query.OrderBy(p => p.EsProgramado && p.FechaProgramada != null ? p.FechaProgramada : p.FechaInicio),
                "precio_desc" => query.OrderByDescending(p => p.Precio),
                "precio_asc" => query.OrderBy(p => p.Precio),
                _ => query.OrderByDescending(p => p.EsProgramado && p.FechaProgramada != null ? p.FechaProgramada : p.FechaInicio)
            };

            var paseos = await query.ToListAsync();

            ViewBag.Estado = estado;
            ViewBag.Busqueda = busqueda;
            ViewBag.Tipo = tipo;
            ViewBag.Orden = orden ?? "fecha_desc";

            return View(paseos);
        }

        // POST: /Paseo/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Duenio")]
        public async Task<IActionResult> Crear(
            int paseadorId,
            List<int> perroIds,
            int duracionMinutos,
            bool esProgramado,
            DateTime? fechaProgramada,
            string? direccionRecogida,
            string? referenciasRecogida,
            string? zonaRecogida,
            string? latitudRecogida,
            string? longitudRecogida)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (perroIds == null || !perroIds.Any())
            {
                TempData["Error"] = "Debes seleccionar al menos un perro para el paseo.";
                return RedirectToAction("Directorio", "Paseador");
            }

            perroIds = perroIds.Distinct().ToList();

            var perros = await _context.Perros
                .Where(p => perroIds.Contains(p.Id))
                .ToListAsync();

            if (perros.Count != perroIds.Count)
            {
                TempData["Error"] = "Uno o más perros seleccionados no existen.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (perros.Any(p => p.DueñoId != usuarioId))
                return Forbid();

            var paseador = await _context.Paseadores
                .FirstOrDefaultAsync(p => p.Id == paseadorId);

            if (paseador == null)
                return NotFound();

            if (duracionMinutos < 15 || duracionMinutos > 180)
            {
                TempData["Error"] = "La duración del paseo debe estar entre 15 y 180 minutos.";
                return RedirectToAction("Directorio", "Paseador");
            }

            direccionRecogida = string.IsNullOrWhiteSpace(direccionRecogida) ? null : direccionRecogida.Trim();
            referenciasRecogida = string.IsNullOrWhiteSpace(referenciasRecogida) ? null : referenciasRecogida.Trim();
            zonaRecogida = string.IsNullOrWhiteSpace(zonaRecogida) ? null : zonaRecogida.Trim();

            if (direccionRecogida == null)
            {
                TempData["Error"] = "Debes escribir la dirección de recolección.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (zonaRecogida == null)
            {
                TempData["Error"] = "Debes seleccionar la zona de recolección.";
                return RedirectToAction("Directorio", "Paseador");
            }

            decimal latitudRecogidaDecimal;
            decimal longitudRecogidaDecimal;

            var latitudValida = decimal.TryParse(
                latitudRecogida,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out latitudRecogidaDecimal);

            var longitudValida = decimal.TryParse(
                longitudRecogida,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out longitudRecogidaDecimal);

            if (!latitudValida || !longitudValida)
            {
                TempData["Error"] = "Debes marcar el punto de recolección en el mapa.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (latitudRecogidaDecimal < -90 || latitudRecogidaDecimal > 90 ||
                longitudRecogidaDecimal < -180 || longitudRecogidaDecimal > 180)
            {
                TempData["Error"] = "La ubicación de recolección no es válida.";
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

            var precioCalculado = CalcularPrecioPaseo(paseador.TarifaPorHora, duracionMinutos);

            if (precioCalculado <= 0)
            {
                TempData["Error"] = "No se pudo calcular el precio del paseo.";
                return RedirectToAction("Directorio", "Paseador");
            }

            if (!paseador.Disponible)
            {
                TempData["Error"] = "El paseador no está disponible en este momento.";
                return RedirectToAction("Directorio", "Paseador");
            }

            var perrosConPaseoActivo = await _context.Paseos
                .Where(p =>
                    (p.Estado == "Pendiente" || p.Estado == "EnCurso") &&
                    (
                        perroIds.Contains(p.PerroId) ||
                        p.PaseoPerros.Any(pp => perroIds.Contains(pp.PerroId))
                    ))
                .Select(p => p.Perro.Nombre)
                .Distinct()
                .ToListAsync();

            if (perrosConPaseoActivo.Any())
            {
                TempData["Error"] = "Uno o más perros ya tienen un paseo pendiente o en curso: " +
                                    string.Join(", ", perrosConPaseoActivo);
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

            var primerPerroId = perroIds.First();

            var paseo = new Paseo
            {
                PaseadorId = paseadorId,
                PerroId = primerPerroId,
                FechaInicio = null,
                FechaFin = null,
                Estado = "Pendiente",
                Precio = precioCalculado,
                DuracionMinutos = duracionMinutos,
                EsProgramado = esProgramado,
                FechaProgramada = esProgramado ? fechaProgramada!.Value.ToUniversalTime() : null,

                DireccionRecogida = direccionRecogida,
                ReferenciasRecogida = referenciasRecogida,
                ZonaRecogida = zonaRecogida,
                LatitudRecogida = latitudRecogidaDecimal,
                LongitudRecogida = longitudRecogidaDecimal,

                FotoInicioUrl = null,
                FotoFinUrl = null
            };

            _context.Paseos.Add(paseo);
            await _context.SaveChangesAsync();

            foreach (var idPerro in perroIds)
            {
                _context.PaseoPerros.Add(new PaseoPerro
                {
                    PaseoId = paseo.Id,
                    PerroId = idPerro
                });
            }

            await _context.SaveChangesAsync();

            TempData["Exito"] = esProgramado
                ? $"Paseo programado correctamente. Precio calculado: ${precioCalculado:0.00}."
                : $"Paseo solicitado correctamente. Precio calculado: ${precioCalculado:0.00}.";

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
                .Include(p => p.PaseoPerros)
                    .ThenInclude(pp => pp.Perro)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paseo == null)
                return NotFound();

            if (paseo.Perro == null || paseo.Paseador == null)
                return NotFound();

            var esDuenio = paseo.Perro.DueñoId == usuarioId ||
                           paseo.PaseoPerros.Any(pp => pp.Perro != null && pp.Perro.DueñoId == usuarioId);

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

        private static decimal CalcularPrecioPaseo(decimal tarifaPorHora, int duracionMinutos)
        {
            return Math.Round(
                tarifaPorHora * (duracionMinutos / 60m),
                2,
                MidpointRounding.AwayFromZero
            );
        }
    }
}