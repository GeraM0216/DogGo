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


namespace DogGo.Controllers
{
    public class AuthController : Controller
    {
        private readonly DogGoDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(DogGoDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Auth/Login
        public IActionResult Login() => View();

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var hash = HashPassword(model.Password);
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == hash);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos");
                return View(model);
            }

            if (!usuario.EmailConfirmado)
            {
                ModelState.AddModelError("", "Debes confirmar tu correo antes de iniciar sesión.");
                return View(model);
            }

            await SignInUser(usuario, model.RememberMe);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/Register
        public IActionResult Register() => View();

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existe = await _context.Usuarios.AnyAsync(u => u.Email == model.Email);
            if (existe)
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con ese email");
                return View(model);
            }

            var codigo = GenerarCodigoConfirmacion();

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                Telefono = model.Telefono,
                Rol = model.Rol,
                PasswordHash = HashPassword(model.Password),
                FechaRegistro = DateTime.UtcNow,
                EmailConfirmado = false,
                CodigoConfirmacion = codigo,
                CodigoExpiraEn = DateTime.UtcNow.AddMinutes(10)
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            if (model.Rol == "Paseador")
            {
                _context.Paseadores.Add(new Paseador
                {
                    UsuarioId = usuario.Id,
                    Descripcion = "",
                    TarifaPorHora = 0,
                    CalificacionPromedio = 0,
                    Disponible = true
                });

                await _context.SaveChangesAsync();
            }

            await _emailService.EnviarCodigoConfirmacionAsync(usuario.Email, codigo);

            TempData["EmailPendiente"] = usuario.Email;

            return RedirectToAction("ConfirmarCorreo");
        }

        // GET: /Auth/ConfirmarCorreo
        public IActionResult ConfirmarCorreo()
        {
            ViewBag.Email = TempData["EmailPendiente"]?.ToString();
            return View();
        }

        // POST: /Auth/ConfirmarCorreo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCorreo(string email, string codigo)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(codigo))
            {
                ViewBag.Error = "Debes capturar el correo y el código.";
                ViewBag.Email = email;
                return View();
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null)
            {
                ViewBag.Error = "No se encontró un usuario con ese correo.";
                ViewBag.Email = email;
                return View();
            }

            if (usuario.EmailConfirmado)
            {
                TempData["Success"] = "Tu correo ya estaba confirmado. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }

            if (usuario.CodigoExpiraEn == null || usuario.CodigoExpiraEn < DateTime.UtcNow)
            {
                ViewBag.Error = "El código ya expiró. Debes solicitar uno nuevo.";
                ViewBag.Email = email;
                return View();
            }

            if (usuario.CodigoConfirmacion != codigo)
            {
                ViewBag.Error = "El código es incorrecto.";
                ViewBag.Email = email;
                return View();
            }

            usuario.EmailConfirmado = true;
            usuario.CodigoConfirmacion = null;
            usuario.CodigoExpiraEn = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Correo confirmado correctamente. Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        //Reenviar codigo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarCodigo(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Debes indicar un correo.";
                return RedirectToAction("ConfirmarCorreo");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null)
            {
                TempData["Error"] = "No se encontró una cuenta con ese correo.";
                return RedirectToAction("ConfirmarCorreo");
            }

            if (usuario.EmailConfirmado)
            {
                TempData["Success"] = "Ese correo ya está confirmado.";
                return RedirectToAction("Login");
            }

            var codigo = GenerarCodigoConfirmacion();
            usuario.CodigoConfirmacion = codigo;
            usuario.CodigoExpiraEn = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            await _emailService.EnviarCodigoConfirmacionAsync(usuario.Email, codigo);

            TempData["EmailPendiente"] = usuario.Email;
            TempData["Success"] = "Se generó un nuevo código de confirmación.";

            return RedirectToAction("ConfirmarCorreo");
        }









        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ── Helpers ──────────────────────────────────────────────

        private async Task SignInUser(Usuario usuario, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                props
            );
        }

        private static string GenerarCodigoConfirmacion()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}