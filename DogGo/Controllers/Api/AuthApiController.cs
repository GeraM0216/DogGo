using System.Security.Claims;
using DogGo.DTOs.Auth;
using DogGo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthApiController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegistrarRequestDto dto)
        {
            var result = await _authService.RegistrarAsync(dto);

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

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Success || result.Data == null)
            {
                return Unauthorized(new
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

        [HttpPost("confirmar-correo")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmarCorreo([FromBody] ConfirmarCorreoRequestDto dto)
        {
            var result = await _authService.ConfirmarCorreoAsync(dto);

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

        [HttpPost("reenviar-codigo")]
        [AllowAnonymous]
        public async Task<IActionResult> ReenviarCodigo([FromBody] ReenviarCodigoRequestDto dto)
        {
            var result = await _authService.ReenviarCodigoAsync(dto);

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

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            var result = await _authService.SolicitarRecuperacionAsync(dto);

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

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);

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

        [HttpGet("perfil")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Perfil()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var perfil = await _authService.ObtenerPerfilAsync(usuarioId);

            if (perfil == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Usuario no encontrado."
                });
            }

            return Ok(new
            {
                success = true,
                data = perfil
            });
        }

        [HttpPut("perfil")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ActualizarPerfil([FromBody] UpdatePerfilRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token inválido."
                });
            }

            var result = await _authService.ActualizarPerfilAsync(usuarioId, dto);

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
    }
}