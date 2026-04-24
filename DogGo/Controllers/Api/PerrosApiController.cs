using System.Security.Claims;
using DogGo.DTOs.Perros;
using DogGo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/perros")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PerrosApiController : ControllerBase
    {
        private readonly PerroService _perroService;

        public PerrosApiController(PerroService perroService)
        {
            _perroService = perroService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerMisPerros()
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var perros = await _perroService.ObtenerMisPerrosAsync(usuarioId.Value);

            return Ok(new
            {
                success = true,
                data = perros
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var perro = await _perroService.ObtenerPorIdAsync(id, usuarioId.Value);

            if (perro == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Perro no encontrado."
                });
            }

            return Ok(new
            {
                success = true,
                data = perro
            });
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] PerroCreateRequestDto dto)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var result = await _perroService.CrearAsync(usuarioId.Value, dto);

            if (!result.Success || result.Data == null)
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] PerroUpdateRequestDto dto)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var result = await _perroService.ActualizarAsync(id, usuarioId.Value, dto);

            if (!result.Success || result.Data == null)
            {
                return NotFound(new
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var result = await _perroService.EliminarAsync(id, usuarioId.Value);

            if (!result.Success)
            {
                return NotFound(new
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

        private int? ObtenerUsuarioIdDesdeToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
            {
                return null;
            }

            return usuarioId;
        }
    }
}