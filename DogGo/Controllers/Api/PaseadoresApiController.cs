using DogGo.Services;
using Microsoft.AspNetCore.Mvc;

namespace DogGo.Controllers.Api
{
    [ApiController]
    [Route("api/paseadores")]
    public class PaseadoresApiController : ControllerBase
    {
        private readonly PaseadorService _paseadorService;

        public PaseadoresApiController(PaseadorService paseadorService)
        {
            _paseadorService = paseadorService;
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

        [HttpGet("{id}")]
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
    }
}