using System.Security.Claims;
using DogGo.DTOs.Paseos;
using DogGo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _paseoService.CrearAsync(usuarioId.Value, dto);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { success = false, message = result.Message });
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
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            var rol = ObtenerRolDesdeToken();

            if (usuarioId == null || string.IsNullOrWhiteSpace(rol))
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var paseos = await _paseoService.ObtenerMisPaseosAsync(usuarioId.Value, rol);

            return Ok(new
            {
                success = true,
                data = paseos
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            var rol = ObtenerRolDesdeToken();

            if (usuarioId == null || string.IsNullOrWhiteSpace(rol))
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var paseo = await _paseoService.ObtenerPorIdAsync(id, usuarioId.Value, rol);

            if (paseo == null)
            {
                return NotFound(new { success = false, message = "Paseo no encontrado." });
            }

            return Ok(new
            {
                success = true,
                data = paseo
            });
        }

        [HttpPut("{id}/aceptar")]
        public async Task<IActionResult> Aceptar(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null) return Unauthorized(new { success = false, message = "Token inválido." });

            var result = await _paseoService.AceptarAsync(id, usuarioId.Value);

            if (!result.Success) return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPut("{id}/rechazar")]
        public async Task<IActionResult> Rechazar(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null) return Unauthorized(new { success = false, message = "Token inválido." });

            var result = await _paseoService.RechazarAsync(id, usuarioId.Value);

            if (!result.Success) return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPut("{id}/iniciar")]
        public async Task<IActionResult> Iniciar(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null) return Unauthorized(new { success = false, message = "Token inválido." });

            var result = await _paseoService.IniciarAsync(id, usuarioId.Value);

            if (!result.Success) return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPut("{id}/finalizar")]
        public async Task<IActionResult> Finalizar(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            if (usuarioId == null) return Unauthorized(new { success = false, message = "Token inválido." });

            var result = await _paseoService.FinalizarAsync(id, usuarioId.Value);

            if (!result.Success) return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPut("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id, [FromBody] object? body = null)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();
            var rol = ObtenerRolDesdeToken();

            if (usuarioId == null || string.IsNullOrWhiteSpace(rol))
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _paseoService.CancelarAsync(id, usuarioId.Value, rol);

            if (!result.Success) return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
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

        private string? ObtenerRolDesdeToken()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}