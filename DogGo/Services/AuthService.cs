using DogGo.Data;
using DogGo.DTOs.Auth;
using DogGo.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DogGo.Services
{
    public class AuthService
    {
        private readonly DogGoDbContext _context;
        private readonly EmailService _emailService;
        private readonly JwtService _jwtService;

        public AuthService(
            DogGoDbContext context,
            EmailService emailService,
            JwtService jwtService)
        {
            _context = context;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        public async Task<(bool Success, string Message)> RegistrarAsync(RegistrarRequestDto dto)
        {
            if (dto == null)
            {
                return (false, "Datos de registro inválidos.");
            }

            if (string.IsNullOrWhiteSpace(dto.Nombre) ||
                string.IsNullOrWhiteSpace(dto.Apellido) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.Rol))
            {
                return (false, "Debes completar nombre, apellido, correo, contraseña y rol.");
            }

            var emailNormalizado = dto.Email.Trim().ToLower();

            var existeUsuario = await _context.Usuarios
                .AnyAsync(u => u.Email.ToLower() == emailNormalizado);

            if (existeUsuario)
            {
                return (false, "Ya existe un usuario con ese correo.");
            }

            var rolNormalizado = NormalizarRol(dto.Rol);
            var codigo = GenerarCodigoConfirmacion();

            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Apellido = dto.Apellido.Trim(),
                Email = emailNormalizado,
                Telefono = dto.Telefono?.Trim() ?? string.Empty,
                Rol = rolNormalizado,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                EmailConfirmado = false,
                CodigoConfirmacion = codigo,
                CodigoExpiraEn = DateTime.UtcNow.AddMinutes(10),
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            if (rolNormalizado == "Paseador")
            {
                var existePaseador = await _context.Paseadores
                    .AnyAsync(p => p.UsuarioId == usuario.Id);

                if (!existePaseador)
                {
                    _context.Paseadores.Add(new Paseador
                    {
                        UsuarioId = usuario.Id,
                        Descripcion = "",
                        TarifaPorHora = 0,
                        CalificacionPromedio = 0,
                        Disponible = true,
                        FotoUrl = "",
                        ZonaServicio = "",
                        ExperienciaAnios = 0
                    });

                    await _context.SaveChangesAsync();
                }
            }

            await EnviarCodigoConfirmacionAsync(usuario.Email, usuario.Nombre, codigo);

            return (true, "Usuario registrado. Revisa tu correo para confirmar tu cuenta.");
        }

        public async Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginRequestDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return (false, "Debes capturar correo y contraseña.", null);
            }

            var emailNormalizado = dto.Email.Trim().ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                return (false, "Correo o contraseña incorrectos.", null);
            }

            var passwordValido = await VerificarPasswordYActualizarSiNecesarioAsync(usuario, dto.Password);

            if (!passwordValido)
            {
                return (false, "Correo o contraseña incorrectos.", null);
            }

            if (!usuario.EmailConfirmado)
            {
                return (false, "Debes confirmar tu correo antes de iniciar sesión.", null);
            }

            var token = _jwtService.GenerarToken(usuario);

            var response = new AuthResponseDto
            {
                Token = token,
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol
            };

            return (true, "Login correcto.", response);
        }

        public async Task<(bool Success, string Message)> ConfirmarCorreoAsync(ConfirmarCorreoRequestDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Codigo))
            {
                return (false, "Debes capturar correo y código.");
            }

            var emailNormalizado = dto.Email.Trim().ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                return (false, "No se encontró un usuario con ese correo.");
            }

            if (usuario.EmailConfirmado)
            {
                return (true, "El correo ya estaba confirmado.");
            }

            if (string.IsNullOrWhiteSpace(usuario.CodigoConfirmacion) || usuario.CodigoExpiraEn == null)
            {
                return (false, "No hay un código de confirmación activo.");
            }

            if (usuario.CodigoExpiraEn < DateTime.UtcNow)
            {
                return (false, "El código ha expirado.");
            }

            if (usuario.CodigoConfirmacion != dto.Codigo.Trim())
            {
                return (false, "El código es incorrecto.");
            }

            usuario.EmailConfirmado = true;
            usuario.CodigoConfirmacion = null;
            usuario.CodigoExpiraEn = null;

            await _context.SaveChangesAsync();

            return (true, "Correo confirmado correctamente.");
        }

        public async Task<(bool Success, string Message)> ReenviarCodigoAsync(ReenviarCodigoRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return (false, "Debes indicar un correo.");
            }

            var emailNormalizado = dto.Email.Trim().ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                return (false, "No se encontró un usuario con ese correo.");
            }

            if (usuario.EmailConfirmado)
            {
                return (false, "Ese correo ya fue confirmado.");
            }

            var nuevoCodigo = GenerarCodigoConfirmacion();
            usuario.CodigoConfirmacion = nuevoCodigo;
            usuario.CodigoExpiraEn = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            await EnviarCodigoConfirmacionAsync(usuario.Email, usuario.Nombre, nuevoCodigo);

            return (true, "Se envió un nuevo código de confirmación.");
        }

        public async Task<(bool Success, string Message)> SolicitarRecuperacionAsync(ForgotPasswordRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return (false, "Debes indicar un correo.");
            }

            var emailNormalizado = dto.Email.Trim().ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                return (false, "No se encontró un usuario con ese correo.");
            }

            var codigo = GenerarCodigoConfirmacion();

            usuario.CodigoRecuperacion = codigo;
            usuario.CodigoRecuperacionExpiraEn = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            await EnviarCodigoRecuperacionAsync(usuario.Email, usuario.Nombre, codigo);

            return (true, "Se enviaron instrucciones de recuperación a tu correo.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            if (dto == null)
            {
                return (false, "Datos inválidos.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Codigo) ||
                string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                return (false, "Debes completar correo, código y nueva contraseña.");
            }

            if (dto.NuevaPassword.Length < 6)
            {
                return (false, "La nueva contraseña debe tener al menos 6 caracteres.");
            }

            var emailNormalizado = dto.Email.Trim().ToLower();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);

            if (usuario == null)
            {
                return (false, "No se encontró una cuenta con ese correo.");
            }

            if (string.IsNullOrWhiteSpace(usuario.CodigoRecuperacion) ||
                usuario.CodigoRecuperacionExpiraEn == null)
            {
                return (false, "No hay un código de recuperación activo. Solicita uno nuevo.");
            }

            if (usuario.CodigoRecuperacionExpiraEn < DateTime.UtcNow)
            {
                return (false, "El código ya expiró. Solicita uno nuevo.");
            }

            if (usuario.CodigoRecuperacion != dto.Codigo.Trim())
            {
                return (false, "El código es incorrecto.");
            }

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            usuario.CodigoRecuperacion = null;
            usuario.CodigoRecuperacionExpiraEn = null;

            await _context.SaveChangesAsync();

            return (true, "Contraseña actualizada correctamente.");
        }

        public async Task<PerfilResponseDto?> ObtenerPerfilAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                return null;
            }

            return new PerfilResponseDto
            {
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Rol = usuario.Rol
            };
        }

        public async Task<(bool Success, string Message, PerfilResponseDto? Data)> ActualizarPerfilAsync(
            int usuarioId,
            UpdatePerfilRequestDto dto)
        {
            if (dto == null)
            {
                return (false, "Datos inválidos.", null);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                return (false, "Usuario no encontrado.", null);
            }

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Apellido = dto.Apellido.Trim();
            usuario.Telefono = dto.Telefono?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            var perfil = new PerfilResponseDto
            {
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Rol = usuario.Rol
            };

            return (true, "Perfil actualizado correctamente.", perfil);
        }

        private async Task<bool> VerificarPasswordYActualizarSiNecesarioAsync(Usuario usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(usuario.PasswordHash))
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
                // Si truena, probablemente era hash viejo SHA256 de la web.
            }

            var hashSha256 = HashPasswordSha256(password);

            if (string.Equals(usuario.PasswordHash, hashSha256, StringComparison.OrdinalIgnoreCase))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        private static string NormalizarRol(string rol)
        {
            var normalizado = rol.Trim().ToLower();

            if (normalizado == "dueño" ||
                normalizado == "duenio" ||
                normalizado == "cliente")
            {
                return "Duenio";
            }

            if (normalizado == "paseador")
            {
                return "Paseador";
            }

            return rol.Trim();
        }

        private static string GenerarCodigoConfirmacion()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private static string HashPasswordSha256(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        private async Task EnviarCodigoConfirmacionAsync(string email, string nombre, string codigo)
        {
            var asunto = "Confirma tu cuenta en DogGo";
            var cuerpo = $@"
                <h2>Hola {nombre}</h2>
                <p>Tu código de confirmación es:</p>
                <h1 style='letter-spacing: 4px;'>{codigo}</h1>
                <p>Este código expira en 10 minutos.</p>";

            await _emailService.EnviarCorreoAsync(email, asunto, cuerpo);
        }

        private async Task EnviarCodigoRecuperacionAsync(string email, string nombre, string codigo)
        {
            var asunto = "Recupera tu contraseña en DogGo";
            var cuerpo = $@"
                <h2>Hola {nombre}</h2>
                <p>Tu código de recuperación es:</p>
                <h1 style='letter-spacing: 4px;'>{codigo}</h1>
                <p>Este código expira en 10 minutos.</p>";

            await _emailService.EnviarCorreoAsync(email, asunto, cuerpo);
        }
    }
}