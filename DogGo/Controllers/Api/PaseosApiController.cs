using System.Security.Claims;
using DogGo.DTOs.Paseos;
using DogGo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/paseos")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PaseosApiController : ControllerBase
    {
        private readonly PaseoService _paseoService;

        public PaseosApiController(PaseoService paseoService)
        {
            _paseoService = paseoService;
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] PaseoCreateRequestDto dto)
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

            var result = await _paseoService.CrearAsync(usuarioId.Value, dto);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("mis-paseos")]
        public async Task<IActionResult> MisPaseos()
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var paseos = await _paseoService.ObtenerMisPaseosAsync(usuarioId.Value, rol);

            return Ok(new
            {
                success = true,
                data = paseos
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var paseo = await _paseoService.ObtenerPorIdAsync(id, usuarioId.Value, rol);

            if (paseo == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Paseo no encontrado."
                });
            }

            return Ok(new
            {
                success = true,
                data = paseo
            });
        }

        [HttpPut("{id:int}/aceptar")]
        public async Task<IActionResult> Aceptar(int id)
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

            var result = await _paseoService.AceptarAsync(id, usuarioId.Value);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPut("{id:int}/rechazar")]
        public async Task<IActionResult> Rechazar(int id)
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

            var result = await _paseoService.RechazarAsync(id, usuarioId.Value);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPut("{id:int}/iniciar")]
        public async Task<IActionResult> Iniciar(int id)
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

            var result = await _paseoService.IniciarAsync(id, usuarioId.Value);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPut("{id:int}/finalizar")]
        public async Task<IActionResult> Finalizar(int id)
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

            var result = await _paseoService.FinalizarAsync(id, usuarioId.Value);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPut("{id:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarPaseoRequestDto? dto = null)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var motivo = dto?.MotivoCancelacion ?? dto?.Motivo ?? dto?.Mensaje;

            var result = await _paseoService.CancelarAsync(id, usuarioId.Value, rol, motivo);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPost("{id:int}/foto-inicio")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> SubirFotoInicio(int id)
        {
            return await SubirFoto(id, "inicio");
        }

        [HttpPost("{id:int}/foto-fin")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> SubirFotoFin(int id)
        {
            return await SubirFoto(id, "fin");
        }

        private async Task<IActionResult> SubirFoto(int id, string tipo)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

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

            if (archivo == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No se recibió ningún archivo."
                });
            }

            var result = await _paseoService.SubirFotoAsync(
                paseoId: id,
                usuarioId: usuarioId.Value,
                rol: rol,
                tipo: tipo,
                archivo: archivo);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost("{id:int}/ubicacion")]
        [HttpPost("{id:int}/tracking")]
        public async Task<IActionResult> EnviarUbicacion(int id, [FromBody] UbicacionRequestDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var latitud = dto.Latitud ?? dto.LatitudActual;
            var longitud = dto.Longitud ?? dto.LongitudActual;

            if (latitud == null || longitud == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Latitud y longitud son obligatorias."
                });
            }

            var result = await _paseoService.EnviarUbicacionAsync(
                paseoId: id,
                usuarioId: usuarioId.Value,
                rol: rol,
                latitud: latitud.Value,
                longitud: longitud.Value);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("{id:int}/ubicacion")]
        [HttpGet("{id:int}/ultima-ubicacion")]
        public async Task<IActionResult> ObtenerUltimaUbicacion(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var result = await _paseoService.ObtenerUltimaUbicacionAsync(
                paseoId: id,
                usuarioId: usuarioId.Value,
                rol: rol);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
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

        private string ObtenerRol()
        {
            return User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? User.FindFirstValue("rol")
                ?? string.Empty;
        }

        public class CancelarPaseoRequestDto
        {
            public string? Motivo { get; set; }
            public string? MotivoCancelacion { get; set; }
            public string? Mensaje { get; set; }
        }

        public class UbicacionRequestDto
        {
            public int? PaseoId { get; set; }
            public decimal? Latitud { get; set; }
            public decimal? Longitud { get; set; }
            public decimal? LatitudActual { get; set; }
            public decimal? LongitudActual { get; set; }
        }
    }
}
