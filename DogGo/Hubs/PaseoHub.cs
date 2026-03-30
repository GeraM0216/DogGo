using DogGo.Data;
using DogGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DogGo.Hubs
{
    [Authorize]
    public class PaseoHub : Hub
    {
        private readonly DogGoDbContext _context;

        public PaseoHub(DogGoDbContext context)
        {
            _context = context;
        }

        // El paseador envía su ubicación cada X segundos
        public async Task EnviarUbicacion(int paseoId, decimal latitud, decimal longitud)
        {
            var usuarioId = int.Parse(
                Context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            // Guardar coordenada en historial
            var ubicacion = new Ubicacion
            {
                PaseoId = paseoId,
                Latitud = latitud,
                Longitud = longitud,
                Timestamp = DateTime.UtcNow
            };
            _context.Ubicaciones.Add(ubicacion);

            // Actualizar coordenada actual en el paseo
            var paseo = await _context.Paseos.FindAsync(paseoId);
            if (paseo != null)
            {
                paseo.LatitudActual = latitud;
                paseo.LongitudActual = longitud;
            }

            await _context.SaveChangesAsync();

            // Transmitir a todos los que observan este paseo
            await Clients.Group(paseoId.ToString())
                .SendAsync("RecibirUbicacion", latitud, longitud);
        }

        // Cambiar estado del paseo
        public async Task CambiarEstado(int paseoId, string nuevoEstado)
        {
            var paseo = await _context.Paseos.FindAsync(paseoId);
            if (paseo == null) return;

            paseo.Estado = nuevoEstado;

            if (nuevoEstado == "EnCurso")
                paseo.FechaInicio = DateTime.UtcNow;
            else if (nuevoEstado == "Finalizado")
                paseo.FechaFin = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await Clients.Group(paseoId.ToString())
                .SendAsync("EstadoCambiado", nuevoEstado);
        }

        // Unirse al grupo del paseo al conectarse
        public async Task UnirseAPaseo(int paseoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, paseoId.ToString());
        }
    }
}