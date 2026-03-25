using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DogGo.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly DogGoDbContext _context;

        public ChatHub(DogGoDbContext context)
        {
            _context = context;
        }

        // Se llama cuando el cliente envía un mensaje
        public async Task EnviarMensaje(int paseoId, int receptorId, string contenido)
        {
            var emisorId = int.Parse(
                Context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            // Guardar en base de datos
            var mensaje = new Mensaje
            {
                EmisorId = emisorId,
                ReceptorId = receptorId,
                PaseoId = paseoId,
                Contenido = contenido,
                FechaEnvio = DateTime.UtcNow,
                Leido = false
            };

            _context.Mensajes.Add(mensaje);
            await _context.SaveChangesAsync();

            // Cargar nombre del emisor para mostrarlo en el chat
            var emisor = await _context.Usuarios.FindAsync(emisorId);
            var nombreEmisor = $"{emisor.Nombre} {emisor.Apellido}";

            // Enviar al grupo del paseo (emisor + receptor lo ven)
            await Clients.Group(paseoId.ToString()).SendAsync("RecibirMensaje", new
            {
                emisorId,
                nombreEmisor,
                contenido,
                fechaEnvio = mensaje.FechaEnvio.ToString("HH:mm"),
                esMio = false   // el cliente lo ajusta según su propio Id
            });
        }

        // Al conectarse, unirse al grupo del paseo
        public async Task UnirseAPaseo(int paseoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, paseoId.ToString());
        }

        // Al desconectarse, salir del grupo
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}