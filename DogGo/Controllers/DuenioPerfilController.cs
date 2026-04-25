using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace DogGo.Controllers
{
    [Authorize(Roles = "Duenio")]
    public class DuenioPerfilController : Controller
    {
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DuenioPerfilController(DogGoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /DuenioPerfil/Editar
        public async Task<IActionResult> Editar()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var perfil = await _context.DuenioPerfiles
                .Include(dp => dp.Usuario)
                .FirstOrDefaultAsync(dp => dp.UsuarioId == usuarioId);

            if (perfil == null)
            {
                perfil = new DuenioPerfil
                {
                    UsuarioId = usuarioId,
                    Usuario = await _context.Usuarios.FirstAsync(u => u.Id == usuarioId)
                };
            }

            return View(perfil);
        }

        // POST: /DuenioPerfil/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            string? direccion,
            string? referenciasDireccion,
            string? zona,
            string? descripcion,
            string? preferenciasPaseo,
            string? latitud,
            string? longitud,
            IFormFile? fotoArchivo)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var perfil = await _context.DuenioPerfiles
                .FirstOrDefaultAsync(dp => dp.UsuarioId == usuarioId);

            if (perfil == null)
            {
                perfil = new DuenioPerfil
                {
                    UsuarioId = usuarioId
                };

                _context.DuenioPerfiles.Add(perfil);
            }

            direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
            referenciasDireccion = string.IsNullOrWhiteSpace(referenciasDireccion) ? null : referenciasDireccion.Trim();
            zona = string.IsNullOrWhiteSpace(zona) ? null : zona.Trim();
            descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
            preferenciasPaseo = string.IsNullOrWhiteSpace(preferenciasPaseo) ? null : preferenciasPaseo.Trim();

            if (direccion == null)
                ModelState.AddModelError("Direccion", "La dirección es obligatoria.");

            if (zona == null)
                ModelState.AddModelError("Zona", "Debes seleccionar una zona.");

            decimal latitudDecimal;
            decimal longitudDecimal;

            var latitudValida = decimal.TryParse(
                latitud,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out latitudDecimal);

            var longitudValida = decimal.TryParse(
                longitud,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out longitudDecimal);

            if (!latitudValida || !longitudValida)
                ModelState.AddModelError("Latitud", "Debes marcar tu ubicación en el mapa.");

            if (latitudValida && (latitudDecimal < -90 || latitudDecimal > 90))
                ModelState.AddModelError("Latitud", "La latitud no es válida.");

            if (longitudValida && (longitudDecimal < -180 || longitudDecimal > 180))
                ModelState.AddModelError("Longitud", "La longitud no es válida.");

            var subioNuevaFoto = fotoArchivo != null && fotoArchivo.Length > 0;

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
                perfil.Direccion = direccion;
                perfil.ReferenciasDireccion = referenciasDireccion;
                perfil.Zona = zona;
                perfil.Descripcion = descripcion;
                perfil.PreferenciasPaseo = preferenciasPaseo;

                if (latitudValida)
                    perfil.Latitud = latitudDecimal;

                if (longitudValida)
                    perfil.Longitud = longitudDecimal;

                return View(perfil);
            }

            perfil.Direccion = direccion;
            perfil.ReferenciasDireccion = referenciasDireccion;
            perfil.Zona = zona;
            perfil.Descripcion = descripcion;
            perfil.PreferenciasPaseo = preferenciasPaseo;
            perfil.Latitud = latitudDecimal;
            perfil.Longitud = longitudDecimal;

            if (subioNuevaFoto)
            {
                var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "duenios");
                Directory.CreateDirectory(carpeta);

                var extension = Path.GetExtension(fotoArchivo!.FileName).ToLowerInvariant();
                var nombreArchivo = Guid.NewGuid().ToString() + extension;
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await fotoArchivo.CopyToAsync(stream);
                }

                perfil.FotoUrl = "/uploads/duenios/" + nombreArchivo;
            }

            await _context.SaveChangesAsync();

            TempData["Exito"] = "Perfil de dueño actualizado correctamente.";
            return RedirectToAction("Editar");
        }
    }
}