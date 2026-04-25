using System.Diagnostics;
using System.Security.Claims;
using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Controllers
{
    public class HomeController : Controller
    {
        private readonly DogGoDbContext _context;

        public HomeController(DogGoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Redirigir paseador con perfil incompleto
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Paseador"))
            {
                var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrWhiteSpace(usuarioIdClaim))
                {
                    var usuarioId = int.Parse(usuarioIdClaim);

                    var paseador = await _context.Paseadores
                        .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                    if (paseador != null && PerfilPaseadorIncompleto(paseador))
                    {
                        TempData["Error"] = "Debes completar tu perfil antes de continuar.";
                        return RedirectToAction("Editar", "Paseador");
                    }
                }
            }

            // Redirigir dueño con perfil incompleto
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Duenio"))
            {
                var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrWhiteSpace(usuarioIdClaim))
                {
                    var usuarioId = int.Parse(usuarioIdClaim);

                    var duenioPerfil = await _context.DuenioPerfiles
                        .FirstOrDefaultAsync(dp => dp.UsuarioId == usuarioId);

                    if (PerfilDuenioIncompleto(duenioPerfil))
                    {
                        TempData["Error"] = "Completa tu perfil de dueño para poder usar tu ubicación predeterminada.";
                        return RedirectToAction("Editar", "DuenioPerfil");
                    }
                }
            }

            // Cargar datos extra solo para dueños
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Duenio"))
            {
                var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrWhiteSpace(usuarioIdStr))
                {
                    var usuarioId = int.Parse(usuarioIdStr);

                    // Paseo en curso o pendiente más reciente
                    var paseoActivo = await _context.Paseos
                        .Where(p =>
                            (p.Perro.DueñoId == usuarioId ||
                             p.PaseoPerros.Any(pp => pp.Perro.DueñoId == usuarioId)) &&
                            (p.Estado == "EnCurso" || p.Estado == "Pendiente"))
                        .Include(p => p.Paseador)
                            .ThenInclude(pa => pa.Usuario)
                        .Include(p => p.Perro)
                        .Include(p => p.PaseoPerros)
                            .ThenInclude(pp => pp.Perro)
                        .OrderBy(p => p.Estado == "EnCurso" ? 0 : 1)
                        .ThenByDescending(p => p.FechaProgramada ?? p.FechaInicio ?? DateTime.MinValue)
                        .FirstOrDefaultAsync();

                    // Último paseo finalizado
                    var ultimoPaseo = await _context.Paseos
                        .Where(p =>
                            (p.Perro.DueñoId == usuarioId ||
                             p.PaseoPerros.Any(pp => pp.Perro.DueñoId == usuarioId)) &&
                            p.Estado == "Finalizado")
                        .Include(p => p.Paseador)
                            .ThenInclude(pa => pa.Usuario)
                        .Include(p => p.Perro)
                        .Include(p => p.PaseoPerros)
                            .ThenInclude(pp => pp.Perro)
                        .Include(p => p.Calificacion)
                        .OrderByDescending(p => p.FechaFin)
                        .FirstOrDefaultAsync();

                    // Conteo total de paseos finalizados
                    var totalPaseos = await _context.Paseos
                        .CountAsync(p =>
                            (p.Perro.DueñoId == usuarioId ||
                             p.PaseoPerros.Any(pp => pp.Perro.DueñoId == usuarioId)) &&
                            p.Estado == "Finalizado");

                    // Perros del dueño
                    var perros = await _context.Perros
                        .Where(p => p.DueñoId == usuarioId)
                        .ToListAsync();

                    ViewBag.PaseoActivo = paseoActivo;
                    ViewBag.UltimoPaseo = ultimoPaseo;
                    ViewBag.TotalPaseos = totalPaseos;
                    ViewBag.Perros = perros;
                }
            }

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        private static bool PerfilPaseadorIncompleto(Paseador paseador)
        {
            return string.IsNullOrWhiteSpace(paseador.Descripcion)
                || string.IsNullOrWhiteSpace(paseador.FotoUrl)
                || string.IsNullOrWhiteSpace(paseador.ZonaServicio)
                || !paseador.ExperienciaAnios.HasValue
                || paseador.ExperienciaAnios.Value < 0
                || paseador.TarifaPorHora <= 0;
        }

        private static bool PerfilDuenioIncompleto(DuenioPerfil? perfil)
        {
            return perfil == null
                || string.IsNullOrWhiteSpace(perfil.Direccion)
                || string.IsNullOrWhiteSpace(perfil.Zona)
                || !perfil.Latitud.HasValue
                || !perfil.Longitud.HasValue;
        }
    }
}