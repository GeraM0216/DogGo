using System.Security.Claims;
using DogGo.DTOs.Chat;
using DogGo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DogGo.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task EnviarMensaje(int paseoId, int receptorId, string contenido)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                throw new HubException("Token inválido.");
            }

            var dto = new EnviarMensajeRequestDto
            {
                PaseoId = paseoId,
                ReceptorId = receptorId,
                Contenido = contenido
            };

            var result = await _chatService.EnviarMensajeAsync(paseoId, usuarioId.Value, rol, dto);

            if (!result.Success || result.Data == null)
            {
                throw new HubException(result.Message);
            }

            await Clients.Group(paseoId.ToString()).SendAsync("RecibirMensaje", new
            {
                id = result.Data.Id,
                paseoId = result.Data.PaseoId,
                emisorId = result.Data.EmisorId,
                receptorId = result.Data.ReceptorId,
                nombreEmisor = result.Data.EmisorNombreCompleto,
                emisorNombre = result.Data.EmisorNombre,
                emisorApellido = result.Data.EmisorApellido,
                emisorRol = result.Data.EmisorRol,
                contenido = result.Data.Contenido,
                fechaEnvio = result.Data.FechaEnvio,
                hora = result.Data.FechaEnvio.ToLocalTime().ToString("HH:mm"),
                leido = result.Data.Leido
            });
        }

        public async Task UnirseAPaseo(int paseoId)
        {
            var usuarioId = ObtenerUsuarioId();
            var rol = ObtenerRol();

            if (usuarioId == null)
            {
                throw new HubException("Token inválido.");
            }

            var permitido = await _chatService.PuedeUnirseAlPaseoAsync(paseoId, usuarioId.Value, rol);

            if (!permitido)
            {
                throw new HubException("No tienes permiso para unirte a este chat.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, paseoId.ToString());
        }

        public async Task SalirDePaseo(int paseoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, paseoId.ToString());
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        private int? ObtenerUsuarioId()
        {
            var valor = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Context.User?.FindFirstValue("sub")
                ?? Context.User?.FindFirstValue("id")
                ?? Context.User?.FindFirstValue("usuarioId")
                ?? Context.User?.FindFirstValue("UsuarioId");

            return int.TryParse(valor, out var usuarioId) ? usuarioId : null;
        }

        private string ObtenerRol()
        {
            return Context.User?.FindFirstValue(ClaimTypes.Role)
                ?? Context.User?.FindFirstValue("role")
                ?? Context.User?.FindFirstValue("rol")
                ?? string.Empty;
        }
    }
}
