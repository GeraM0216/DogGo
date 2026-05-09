using System.Security.Claims;
using DogGo.DTOs.Calificaciones;
using DogGo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/calificaciones")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CalificacionesApiController : ControllerBase
    {
        private readonly CalificacionService _calificacionService;

        public CalificacionesApiController(CalificacionService calificacionService)
        {
            _calificacionService = calificacionService;
        }

        [HttpPost("paseo/{paseoId:int}")]
        [HttpPost("paseos/{paseoId:int}")]
        public async Task<IActionResult> CalificarPaseo(int paseoId, [FromBody] CrearCalificacionRequestDto dto)
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

            var result = await _calificacionService.CrearAsync(paseoId, usuarioId.Value, rol, dto);

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

        [HttpPost]
        public async Task<IActionResult> Calificar([FromBody] CrearCalificacionRequestDto dto)
        {
            if (dto.PaseoId == null || dto.PaseoId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "El paseoId es obligatorio."
                });
            }

            return await CalificarPaseo(dto.PaseoId.Value, dto);
        }

        [HttpGet("paseo/{paseoId:int}")]
        [HttpGet("paseos/{paseoId:int}")]
        public async Task<IActionResult> ObtenerPorPaseo(int paseoId)
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

            var result = await _calificacionService.ObtenerPorPaseoAsync(paseoId, usuarioId.Value, rol);

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
                message = result.Message,
                data = result.Data
            });
        }

        [AllowAnonymous]
        [HttpGet("paseador/{paseadorId:int}")]
        [HttpGet("paseadores/{paseadorId:int}")]
        public async Task<IActionResult> ObtenerPorPaseador(int paseadorId)
        {
            var result = await _calificacionService.ObtenerPorPaseadorAsync(paseadorId);

            if (!result.Success)
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message,
                    data = result.Data
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
    }
}
