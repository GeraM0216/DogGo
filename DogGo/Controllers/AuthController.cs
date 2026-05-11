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
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Codigo { get; set; } = string.Empty;
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

            if (!usuario.Activo)
            {
                ModelState.AddModelError("", "Esta cuenta está desactivada. Contacta al administrador del sistema.");
                return View(model);
            }

            if (!usuario.EmailConfirmado)
            {
                TempData["Error"] = "Tu cuenta no está confirmada.";
                return RedirectToAction("ConfirmarEmail", new { email = usuario.Email });
            }

            await SignInUser(usuario, model.RememberMe);

            return RedirigirPorRol(usuario);
        }

        // --- REGISTRO ---
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var emailNormalizado = model.Email.Trim().ToLower();

            if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == emailNormalizado))
            {
                ModelState.AddModelError("Email", "Este email ya está registrado.");
                return View(model);
            }

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = emailNormalizado,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Telefono = model.Telefono ?? "",
                Rol = model.Rol,
                EmailConfirmado = false,
                CodigoConfirmacion = GenerarCodigoConfirmacion(),
                CodigoExpiraEn = DateTime.UtcNow.AddMinutes(10),
                FechaRegistro = DateTime.UtcNow,
                Activo = true,
                FechaDesactivacion = null,
                MotivoDesactivacion = null
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Si es paseador, crear su perfil base
            if (usuario.Rol == "Paseador")
            {
                _context.Paseadores.Add(new Paseador
                {
                    UsuarioId = usuario.Id,
                    Disponible = true,
                    TarifaPorHora = 0,
                    Descripcion = "¡Hola! Soy un nuevo paseador."
                });

                await _context.SaveChangesAsync();
            }

            try
            {
                await _emailService.EnviarCodigoConfirmacionAsync(
                    usuario.Email,
                    usuario.CodigoConfirmacion ?? string.Empty
                );
            }
            catch
            {
                // Ignorar errores de correo en local.
            }

            return RedirectToAction("ConfirmarEmail", new { email = usuario.Email });
        }

        // --- CONFIRMACIÓN ---
        public IActionResult ConfirmarEmail(string email)
        {
            return View("ConfirmarCorreo", new ConfirmacionAuxiliar { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEmail(ConfirmacionAuxiliar model)
        {
            if (!ModelState.IsValid)
            {
                return View("ConfirmarCorreo", model);
            }

            var emailNormalizado = model.Email.Trim().ToLower();
            var codigo = model.Codigo.Trim();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == emailNormalizado &&
                    u.CodigoConfirmacion == codigo
                );

            if (usuario == null)
            {
                ModelState.AddModelError("", "Código incorrecto o expirado.");
                return View("ConfirmarCorreo", model);
            }

            if (usuario.CodigoExpiraEn != null && usuario.CodigoExpiraEn < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "El código ya expiró.");
                return View("ConfirmarCorreo", model);
            }

            if (!usuario.Activo)
            {
                TempData["Error"] = "Esta cuenta está desactivada. Contacta al administrador del sistema.";
                return RedirectToAction("Login");
            }

            usuario.EmailConfirmado = true;
            usuario.CodigoConfirmacion = null;
            usuario.CodigoExpiraEn = null;

            await _context.SaveChangesAsync();

            await SignInUser(usuario, false);

            return RedirigirPorRol(usuario);
        }

        // --- RECUPERAR CONTRASEÑA ---
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Debes escribir tu correo electrónico.";
                return View();
            }

            if (email.Trim().Length > 120)
            {
                ViewBag.Error = "El correo no puede superar 120 caracteres.";
                return View();
            }

            var emailNormalizado = email.Trim().ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                ViewBag.Error = "No se encontró una cuenta con ese correo.";
                return View();
            }

            if (!usuario.Activo)
            {
                ViewBag.Error = "Esta cuenta está desactivada. Contacta al administrador del sistema.";
                return View();
            }

            var codigo = GenerarCodigoConfirmacion();

            usuario.CodigoRecuperacion = codigo;
            usuario.CodigoRecuperacionExpiraEn = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            try
            {
                await EnviarCodigoRecuperacionAsync(usuario.Email, usuario.Nombre, codigo);
            }
            catch
            {
                ViewBag.Error = "No se pudo enviar el correo de recuperación. Intenta de nuevo.";
                return View();
            }

            TempData["Success"] = "Te enviamos un código de recuperación a tu correo.";
            return RedirectToAction("ResetPassword", new { email = usuario.Email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string email,
            string codigo,
            string nuevaPassword,
            string confirmarPassword)
        {
            ViewBag.Email = email;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(codigo) ||
                string.IsNullOrWhiteSpace(nuevaPassword) ||
                string.IsNullOrWhiteSpace(confirmarPassword))
            {
                ViewBag.Error = "Debes completar todos los campos.";
                return View();
            }

            if (email.Trim().Length > 120)
            {
                ViewBag.Error = "El correo no puede superar 120 caracteres.";
                return View();
            }

            if (codigo.Trim().Length != 6)
            {
                ViewBag.Error = "El código debe tener 6 dígitos.";
                return View();
            }

            if (nuevaPassword != confirmarPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View();
            }

            if (nuevaPassword.Length < 6)
            {
                ViewBag.Error = "La nueva contraseña debe tener al menos 6 caracteres.";
                return View();
            }

            if (nuevaPassword.Length > 100)
            {
                ViewBag.Error = "La nueva contraseña no puede superar 100 caracteres.";
                return View();
            }

            var emailNormalizado = email.Trim().ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                ViewBag.Error = "No se encontró una cuenta con ese correo.";
                return View();
            }

            if (!usuario.Activo)
            {
                ViewBag.Error = "Esta cuenta está desactivada. Contacta al administrador del sistema.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(usuario.CodigoRecuperacion) ||
                usuario.CodigoRecuperacionExpiraEn == null)
            {
                ViewBag.Error = "No hay un código de recuperación activo. Solicita uno nuevo.";
                return View();
            }

            if (usuario.CodigoRecuperacionExpiraEn < DateTime.UtcNow)
            {
                ViewBag.Error = "El código ya expiró. Solicita uno nuevo.";
                return View();
            }

            if (usuario.CodigoRecuperacion != codigo.Trim())
            {
                ViewBag.Error = "El código es incorrecto.";
                return View();
            }

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);
            usuario.CodigoRecuperacion = null;
            usuario.CodigoRecuperacionExpiraEn = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Contraseña actualizada correctamente. Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        // --- SWITCH DE ROLES (CAMBIO DE MODO) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchView()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login");
            }

            var usuario = await _context.Usuarios.FindAsync(int.Parse(userIdStr));

            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            if (!usuario.Activo)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Error"] = "Esta cuenta está desactivada. Contacta al administrador del sistema.";
                return RedirectToAction("Login");
            }

            usuario.Rol = usuario.Rol == "Duenio" ? "Paseador" : "Duenio";

            if (usuario.Rol == "Paseador")
            {
                var existe = await _context.Paseadores
                    .AnyAsync(p => p.UsuarioId == usuario.Id);

                if (!existe)
                {
                    _context.Paseadores.Add(new Paseador
                    {
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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = rememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                props
            );
        }

        private IActionResult RedirigirPorRol(Usuario usuario)
        {
            if (usuario.Rol == "SuperAdmin")
            {
                return RedirectToAction("Index", "SuperAdmin");
            }

            if (usuario.Rol == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<bool> VerificarPasswordYActualizarSiNecesarioAsync(
            Usuario usuario,
            string password)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(usuario.PasswordHash))
            {
                return false;
            }

            try
            {
                if (BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
                {
                    return true;
                }
            }
            catch
            {
                // Si truena, probablemente era hash viejo SHA256.
            }

            var hashSha256 = HashPasswordSha256(password);

            if (string.Equals(
                    usuario.PasswordHash,
                    hashSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        private static string GenerarCodigoConfirmacion()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        private async Task EnviarCodigoRecuperacionAsync(
            string email,
            string nombre,
            string codigo)
        {
            var asunto = "Recupera tu contraseña en DogGo";

            var cuerpo = $@"
                <h2>Hola {nombre}</h2>
                <p>Recibimos una solicitud para recuperar tu contraseña.</p>
                <p>Tu código de recuperación es:</p>
                <h1 style='letter-spacing: 4px;'>{codigo}</h1>
                <p>Este código expira en 10 minutos.</p>
                <p>Si tú no solicitaste este cambio, puedes ignorar este correo.</p>";

            await _emailService.EnviarCorreoAsync(email, asunto, cuerpo);
        }

        private static string HashPasswordSha256(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}