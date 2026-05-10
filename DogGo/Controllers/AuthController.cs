using DogGo.Data;
using DogGo.Models;
using DogGo.Services;
using DogGo.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DogGo.Controllers
{
    // Modelo auxiliar para que la verificación no dependa de archivos externos
    public class ConfirmacionAuxiliar
    {
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string Codigo { get; set; } = string.Empty;
    }

    public class AuthController : Controller
    {
        private readonly DogGoDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(DogGoDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // --- LOGIN ---
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var emailNormalizado = model.Email.Trim().ToLower();
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos");
                return View(model);
            }

            var passwordValido = await VerificarPasswordYActualizarSiNecesarioAsync(usuario, model.Password);
            if (!passwordValido)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos");
                return View(model);
            }

            if (!usuario.EmailConfirmado)
            {
                TempData["Error"] = "Tu cuenta no está confirmada.";
                return RedirectToAction("ConfirmarEmail", new { email = usuario.Email });
            }

            await SignInUser(usuario, model.RememberMe);
            return (usuario.Rol == "Admin") ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Home");
        }

        // --- REGISTRO ---
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Este email ya está registrado.");
                return View(model);
            }

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Telefono = model.Telefono ?? "",
                Rol = model.Rol,
                EmailConfirmado = false,
                CodigoConfirmacion = GenerarCodigoConfirmacion(),
                CodigoExpiraEn = DateTime.UtcNow.AddMinutes(10),
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Si es paseador, crear su perfil base
            if (usuario.Rol == "Paseador")
            {
                _context.Paseadores.Add(new Paseador { UsuarioId = usuario.Id, Disponible = true, TarifaPorHora = 0, Descripcion = "¡Hola! Soy un nuevo paseador." });
                await _context.SaveChangesAsync();
            }

            try
            {
                await _emailService.EnviarCodigoConfirmacionAsync(usuario.Email, usuario.CodigoConfirmacion);
            }
            catch { /* Ignorar errores de correo en local */ }

            return RedirectToAction("ConfirmarEmail", new { email = usuario.Email });
        }

        // --- CONFIRMACIÓN (Usando tu archivo VerificarCodigo.cshtml) ---
        public IActionResult ConfirmarEmail(string email)
        {
            return View("VerificarCodigo", new ConfirmacionAuxiliar { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEmail(ConfirmacionAuxiliar model)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.CodigoConfirmacion == model.Codigo);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Código incorrecto o expirado.");
                return View("VerificarCodigo", model);
            }

            usuario.EmailConfirmado = true;
            usuario.CodigoConfirmacion = null;
            await _context.SaveChangesAsync();

            await SignInUser(usuario, false);
            return RedirectToAction("Index", "Home");
        }

        // --- SWITCH DE ROLES (CAMBIO DE MODO) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchView()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            var usuario = await _context.Usuarios.FindAsync(int.Parse(userIdStr));
            if (usuario == null) return RedirectToAction("Login");

            // Cambiar rol
            usuario.Rol = (usuario.Rol == "Duenio") ? "Paseador" : "Duenio";

            // Si pasa a paseador y no tiene perfil, crearlo
            if (usuario.Rol == "Paseador")
            {
                var existe = await _context.Paseadores.AnyAsync(p => p.UsuarioId == usuario.Id);
                if (!existe)
                {
                    _context.Paseadores.Add(new Paseador { 
                        UsuarioId = usuario.Id, 
                        Disponible = true, 
                        Descripcion = "Perfil activado vía switch", 
                        TarifaPorHora = 0 
                    });
                }
            }

            await _context.SaveChangesAsync();
            await SignInUser(usuario, false);

            TempData["Exito"] = $"Modo {(usuario.Rol == "Duenio" ? "Cliente" : "Paseador")} activado";
            return RedirectToAction("Index", "Home");
        }

        // --- LOGOUT ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // --- MÉTODOS PRIVADOS DE APOYO ---
        private async Task SignInUser(Usuario usuario, bool rememberMe)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties { IsPersistent = rememberMe };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        }

        private async Task<bool> VerificarPasswordYActualizarSiNecesarioAsync(Usuario usuario, string password)
        {
            try { if (BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash)) return true; } catch { }

            var hashSha256 = HashPasswordSha256(password);
            if (string.Equals(usuario.PasswordHash, hashSha256, StringComparison.OrdinalIgnoreCase))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        private static string GenerarCodigoConfirmacion() => new Random().Next(100000, 999999).ToString();

        private static string HashPasswordSha256(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}