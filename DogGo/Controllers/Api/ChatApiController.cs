using System.Security.Claims;
using DogGo.DTOs.Chat;
using DogGo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/chat")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatApiController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatApiController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("paseo/{paseoId:int}")]
        [HttpGet("paseos/{paseoId:int}/mensajes")]
        public async Task<IActionResult> ObtenerMensajes(int paseoId)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _chatService.ObtenerMensajesAsync(paseoId, usuarioId.Value, rol);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpPost("paseo/{paseoId:int}/mensajes")]
        [HttpPost("paseos/{paseoId:int}/mensajes")]
        public async Task<IActionResult> EnviarMensajeEnPaseo(int paseoId, [FromBody] EnviarMensajeRequestDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _chatService.EnviarMensajeAsync(paseoId, usuarioId.Value, rol, dto);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> EnviarMensaje([FromBody] EnviarMensajeRequestDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            if (dto.PaseoId == null || dto.PaseoId <= 0)
            {
                return BadRequest(new { success = false, message = "El paseoId es obligatorio." });
            }

            var result = await _chatService.EnviarMensajeAsync(dto.PaseoId.Value, usuarioId.Value, rol, dto);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpPut("paseo/{paseoId:int}/leidos")]
        [HttpPut("paseos/{paseoId:int}/leidos")]
        public async Task<IActionResult> MarcarComoLeidos(int paseoId)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                return Unauthorized(new { success = false, message = "Token inválido." });
            }

            var result = await _chatService.MarcarComoLeidosAsync(paseoId, usuarioId.Value, rol);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
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
