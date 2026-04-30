using DogGo.Data;
using DogGo.DTOs.Auth;
using DogGo.Models;
using Microsoft.EntityFrameworkCore;

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
            var existeUsuario = await _context.Usuarios
                .AnyAsync(u => u.Email == dto.Email);

            if (existeUsuario)
            {
                return (false, "Ya existe un usuario con ese correo.");
            }

            var codigo = GenerarCodigoConfirmacion();

            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                Telefono = dto.Telefono,
                Rol = dto.Rol,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                EmailConfirmado = false,
                CodigoConfirmacion = codigo,
                CodigoExpiraEn = DateTime.UtcNow.AddMinutes(10),
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            await EnviarCodigoConfirmacionAsync(usuario.Email, usuario.Nombre, codigo);

            return (true, "Usuario registrado. Revisa tu correo para confirmar tu cuenta.");
        }

        public async Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginRequestDto dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null)
            {
                return (false, "Correo o contraseña incorrectos.", null);
            }

            bool passwordValido;

            try
            {
                passwordValido = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            }
            catch
            {
                return (false, "La contraseña almacenada de este usuario no tiene un formato válido. Crea de nuevo el usuario o actualiza su contraseña.", null);
            }

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
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

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

            if (usuario.CodigoConfirmacion != dto.Codigo)
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
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

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
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

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

        private static string GenerarCodigoConfirmacion()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
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