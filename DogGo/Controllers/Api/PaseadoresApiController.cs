using System.Security.Claims;
using DogGo.Data;
using DogGo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/paseadores")]
    public class PaseadoresApiController : ControllerBase
    {
        private readonly PaseadorService _paseadorService;
        private readonly DogGoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PaseadoresApiController(
            PaseadorService paseadorService,
            DogGoDbContext context,
            IWebHostEnvironment environment)
        {
            _paseadorService = paseadorService;
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var paseadores = await _paseadorService.ObtenerTodosAsync();

            return Ok(new
            {
                success = true,
                data = paseadores
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var paseador = await _paseadorService.ObtenerPorIdAsync(id);

            if (paseador == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Paseador no encontrado."
                });
            }

            return Ok(new
            {
                success = true,
                data = paseador
            });
        }

        [HttpGet("mi-perfil")]
        [HttpGet("perfil")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

            var paseador = await _context.Paseadores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId.Value);

            if (paseador == null)
            {
                return Ok(new
                {
                    success = true,
                    message = "El perfil de paseador todavía no está completo.",
                    data = new
                    {
                        existe = false,
                        usuarioId = usuario.Id,
                        nombre = usuario.Nombre,
                        apellido = usuario.Apellido,
                        nombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim(),
                        email = usuario.Email,
                        telefono = usuario.Telefono,
                        descripcion = "",
                        zonaServicio = "",
                        tarifaPorHora = 0,
                        experienciaAnios = 0,
                        fotoUrl = (string?)null,
                        imagenUrl = (string?)null,
                        disponible = false,
                        perfilCompleto = false
                    }
                });
            }

            return Ok(new
            {
                success = true,
                data = MapPaseadorPerfil(paseador)
            });
        }

        [HttpPost("mi-perfil")]
        [HttpPut("mi-perfil")]
        [HttpPost("perfil")]
        [HttpPut("perfil")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GuardarMiPerfil([FromBody] GuardarPerfilPaseadorDto dto)
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

            var rol = (usuario.Rol ?? "").Trim();

            if (!rol.Equals("Paseador", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Solo usuarios con rol Paseador pueden completar este perfil."
                });
            }

            var descripcion = (dto.Descripcion ?? "").Trim();
            var zonaServicio = (dto.ZonaServicio ?? dto.Zona ?? "").Trim();
            var tarifaPorHora = dto.TarifaPorHora ?? dto.Tarifa ?? 0;
            var experienciaAnios = dto.ExperienciaAnios ?? dto.Experiencia ?? 0;
            var disponible = dto.Disponible ?? true;

            if (descripcion.Length > 500)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La descripción no puede superar 500 caracteres."
                });
            }

            if (zonaServicio.Length > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La zona de servicio no puede superar 100 caracteres."
                });
            }

            if (tarifaPorHora < 0 || tarifaPorHora > 100000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La tarifa por hora debe estar entre 0 y 100000."
                });
            }

            if (experienciaAnios < 0 || experienciaAnios > 80)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Los años de experiencia deben estar entre 0 y 80."
                });
            }

            var paseador = await _context.Paseadores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId.Value);

            if (paseador == null)
            {
                paseador = new DogGo.Models.Paseador
                {
                    UsuarioId = usuarioId.Value,
                    Descripcion = descripcion,
                    ZonaServicio = zonaServicio,
                    TarifaPorHora = tarifaPorHora,
                    ExperienciaAnios = experienciaAnios,
                    Disponible = disponible,
                    CalificacionPromedio = 0
                };

                _context.Paseadores.Add(paseador);
            }
            else
            {
                paseador.Descripcion = descripcion;
                paseador.ZonaServicio = zonaServicio;
                paseador.TarifaPorHora = tarifaPorHora;
                paseador.ExperienciaAnios = experienciaAnios;
                paseador.Disponible = disponible;
            }

            await _context.SaveChangesAsync();

            var actualizado = await _context.Paseadores
                .Include(p => p.Usuario)
                .FirstAsync(p => p.UsuarioId == usuarioId.Value);

            return Ok(new
            {
                success = true,
                message = "Perfil de paseador guardado correctamente.",
                data = MapPaseadorPerfil(actualizado)
            });
        }

        [HttpPost("mi-perfil/foto")]
        [HttpPost("perfil/foto")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5_000_000)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SubirFotoMiPerfil()
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

            if (!Request.HasFormContentType)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La solicitud debe ser multipart/form-data."
                });
            }

            var archivo = Request.Form.Files.FirstOrDefault();

            if (archivo == null || archivo.Length <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No se recibió ningún archivo."
                });
            }

            if (archivo.Length > 5_000_000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La imagen no puede superar 5 MB."
                });
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!permitidas.Contains(extension))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Formato no permitido. Usa jpg, jpeg, png o webp."
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

            var rol = (usuario.Rol ?? "").Trim();

            if (!rol.Equals("Paseador", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Solo usuarios con rol Paseador pueden subir foto de paseador."
                });
            }

            var paseador = await _context.Paseadores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId.Value);

            if (paseador == null)
            {
                paseador = new DogGo.Models.Paseador
                {
                    UsuarioId = usuarioId.Value,
                    Descripcion = "",
                    ZonaServicio = "",
                    TarifaPorHora = 0,
                    ExperienciaAnios = 0,
                    Disponible = false,
                    CalificacionPromedio = 0
                };

                _context.Paseadores.Add(paseador);
                await _context.SaveChangesAsync();
            }

            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var carpeta = Path.Combine(webRoot, "uploads", "paseadores");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"paseador-{paseador.Id}-{Guid.NewGuid():N}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            await using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            paseador.FotoUrl = $"/uploads/paseadores/{nombreArchivo}";

            await _context.SaveChangesAsync();

            var actualizado = await _context.Paseadores
                .Include(p => p.Usuario)
                .FirstAsync(p => p.UsuarioId == usuarioId.Value);

            return Ok(new
            {
                success = true,
                message = "Foto de paseador actualizada correctamente.",
                data = MapPaseadorPerfil(actualizado)
            });
        }

        private int? ObtenerUsuarioId()
        {
            var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("usuarioId")
                ?? User.FindFirstValue("UsuarioId");

            return int.TryParse(valor, out var usuarioId) ? usuarioId : null;
        }

        private static object MapPaseadorPerfil(DogGo.Models.Paseador paseador)
        {
            var usuario = paseador.Usuario;
            var nombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim();

            var perfilCompleto =
                !string.IsNullOrWhiteSpace(paseador.Descripcion) &&
                !string.IsNullOrWhiteSpace(paseador.ZonaServicio) &&
                paseador.TarifaPorHora > 0;

            return new
            {
                existe = true,
                id = paseador.Id,
                paseadorId = paseador.Id,
                usuarioId = paseador.UsuarioId,

                nombre = usuario.Nombre,
                apellido = usuario.Apellido,
                nombreCompleto,
                email = usuario.Email,
                telefono = usuario.Telefono,

                descripcion = paseador.Descripcion,
                zonaServicio = paseador.ZonaServicio,
                experienciaAnios = paseador.ExperienciaAnios,
                tarifaPorHora = paseador.TarifaPorHora,
                calificacionPromedio = paseador.CalificacionPromedio,
                disponible = paseador.Disponible,

                fotoUrl = paseador.FotoUrl,
                imagenUrl = paseador.FotoUrl,

                perfilCompleto
            };
        }

        public class GuardarPerfilPaseadorDto
        {
            public string? Descripcion { get; set; }
            public string? ZonaServicio { get; set; }
            public string? Zona { get; set; }

            public decimal? TarifaPorHora { get; set; }
            public decimal? Tarifa { get; set; }

            public int? ExperienciaAnios { get; set; }
            public int? Experiencia { get; set; }

            public bool? Disponible { get; set; }
        }
    }
}