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
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var perros = await _perroService.ObtenerMisPerrosAsync(usuarioId.Value);

            return Ok(new { success = true, data = perros });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var perro = await _perroService.ObtenerPorIdAsync(id, usuarioId.Value);

            if (perro == null)
            {
                return NotFound(new { success = false, message = "Perro no encontrado." });
            }

            return Ok(new { success = true, data = perro });
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] PerroCreateRequestDto dto)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _perroService.CrearAsync(usuarioId.Value, dto);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] PerroUpdateRequestDto dto)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _perroService.ActualizarAsync(id, usuarioId.Value, dto);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpPost("{id:int}/foto")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> SubirFoto(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            if (!Request.HasFormContentType)
            {
                return BadRequest(new { success = false, message = "La solicitud debe ser multipart/form-data." });
            }

            var archivo = Request.Form.Files.FirstOrDefault();

            if (archivo == null)
            {
                return BadRequest(new { success = false, message = "No se recibió ningún archivo." });
            }

            var result = await _perroService.SubirFotoAsync(id, usuarioId.Value, archivo);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _perroService.EliminarAsync(id, usuarioId.Value);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        private int? ObtenerUsuarioIdDesdeToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst("usuarioId")?.Value
                ?? User.FindFirst("UsuarioId")?.Value;

            return int.TryParse(userIdClaim, out var usuarioId) ? usuarioId : null;
        }
    }
}
