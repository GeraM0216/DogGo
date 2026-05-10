using System.Globalization;
using System.Security.Claims;
using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/duenio-perfil")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Duenio,Dueño,Admin"
    )]
    public class DuenioPerfilApiController : ControllerBase
    {
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DuenioPerfilApiController(
            DogGoDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);

            if (usuario == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Usuario no encontrado."
                });
            }

            var perfil = await ObtenerOCrearPerfil(usuarioId.Value);

            return Ok(new
            {
                success = true,
                data = new
                {
                    perfil.Id,
                    perfil.UsuarioId,
                    perfil.FotoUrl,
                    perfil.Direccion,
                    perfil.ReferenciasDireccion,
                    perfil.Zona,
                    perfil.Latitud,
                    perfil.Longitud,
                    perfil.Descripcion,
                    perfil.PreferenciasPaseo,
                    usuario = new
                    {
                        usuario.Id,
                        usuario.Nombre,
                        usuario.Apellido,
                        usuario.Email,
                        usuario.Telefono,
                        usuario.Rol,
                        usuario.EmailConfirmado
                    }
                }
            });
        }

        [HttpPut("mi-perfil")]
        public async Task<IActionResult> ActualizarMiPerfil([FromBody] ActualizarDuenioPerfilRequest request)
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var perfil = await ObtenerOCrearPerfil(usuarioId.Value);

            perfil.Direccion = Limpiar(request.Direccion);
            perfil.ReferenciasDireccion = Limpiar(request.ReferenciasDireccion);
            perfil.Zona = Limpiar(request.Zona);
            perfil.Descripcion = Limpiar(request.Descripcion);
            perfil.PreferenciasPaseo = Limpiar(request.PreferenciasPaseo);

            if (request.Latitud.HasValue)
            {
                if (request.Latitud.Value < -90 || request.Latitud.Value > 90)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La latitud no es válida."
                    });
                }

                perfil.Latitud = request.Latitud.Value;
            }

            if (request.Longitud.HasValue)
            {
                if (request.Longitud.Value < -180 || request.Longitud.Value > 180)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La longitud no es válida."
                    });
                }

                perfil.Longitud = request.Longitud.Value;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Perfil de dueño actualizado correctamente.",
                data = new
                {
                    perfil.Id,
                    perfil.UsuarioId,
                    perfil.FotoUrl,
                    perfil.Direccion,
                    perfil.ReferenciasDireccion,
                    perfil.Zona,
                    perfil.Latitud,
                    perfil.Longitud,
                    perfil.Descripcion,
                    perfil.PreferenciasPaseo
                }
            });
        }

        [HttpPost("foto")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> SubirFoto([FromForm] IFormFile? foto)
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            if (foto == null || foto.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Debes seleccionar una foto."
                });
            }

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Solo se permiten imágenes JPG, JPEG, PNG o WEBP."
                });
            }

            if (foto.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La imagen no debe superar los 5 MB."
                });
            }

            var perfil = await ObtenerOCrearPerfil(usuarioId.Value);

            var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "duenios");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            perfil.FotoUrl = $"/uploads/duenios/{nombreArchivo}";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Foto de perfil actualizada correctamente.",
                data = new
                {
                    perfil.Id,
                    perfil.UsuarioId,
                    perfil.FotoUrl
                }
            });
        }

        private async Task<DuenioPerfil> ObtenerOCrearPerfil(int usuarioId)
        {
            var perfil = await _context.DuenioPerfiles
                .FirstOrDefaultAsync(dp => dp.UsuarioId == usuarioId);

            if (perfil != null)
            {
                return perfil;
            }

            perfil = new DuenioPerfil
            {
                UsuarioId = usuarioId
            };

            _context.DuenioPerfiles.Add(perfil);
            await _context.SaveChangesAsync();

            return perfil;
        }

        private int? ObtenerUsuarioId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst("usuarioId")?.Value
                ?? User.FindFirst("UsuarioId")?.Value;

            return int.TryParse(userIdClaim, out var usuarioId) ? usuarioId : null;
        }

        private static string? Limpiar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }

    public class ActualizarDuenioPerfilRequest
    {
        public string? Direccion { get; set; }
        public string? ReferenciasDireccion { get; set; }
        public string? Zona { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? Descripcion { get; set; }
        public string? PreferenciasPaseo { get; set; }
    }
}