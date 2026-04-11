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
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Paseador"))
            {
                var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrWhiteSpace(usuarioIdClaim))
                {
                    var usuarioId = int.Parse(usuarioIdClaim);

                    var paseador = await _context.Paseadores
                        .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

                    if (paseador != null && PerfilIncompleto(paseador))
                    {
                        TempData["Error"] = "Debes completar tu perfil antes de continuar.";
                        return RedirectToAction("Editar", "Paseador");
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        private static bool PerfilIncompleto(Paseador paseador)
        {
            return string.IsNullOrWhiteSpace(paseador.Descripcion)
                || string.IsNullOrWhiteSpace(paseador.FotoUrl)
                || string.IsNullOrWhiteSpace(paseador.ZonaServicio)
                || !paseador.ExperienciaAnios.HasValue
                || paseador.ExperienciaAnios.Value < 0
                || paseador.TarifaPorHora <= 0;
        }
    }
}